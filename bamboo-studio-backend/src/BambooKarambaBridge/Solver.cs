using Rhino.Geometry;
using BambooStages123;
using Bamboo45;
using BambooCompare;
using System.Text.Json;
namespace BambooStudio;
public static class Solver {
    public static string Scope => "Karamba3D 求解；"+Engine.Scope+" 位移阈值为用户值，0 时用 W/250；强度按用户 Fc/Ft/Fb 进行轴力与弯曲组合初筛，三项全 0 时未评估；这些应用筛选规则不是 Karamba 自动规范验算。";
    public static object Run(Input p,Job job){
        var spec=new Spec{Shape=p.I("type")%2==0?1:0,Structure=p.I("type")/2,Foot=p.I("Foot"),Units=2,W=p.N("W"),H=p.N("H"),LegH=p.I("Foot")==0?0:p.N("LegH"),Angle=p.I("Foot")==2?p.N("LegAngle"):0,CrossAngle=p.N("CrossAngle",25),Depth=p.N("TrussDepth",600),Panels=p.I("Panels",8),PointedVersion=p.I("PointedVersion"),PointFootGap=p.N("PointFootGap",10),PointSlope=p.N("PointSlope",.9),PointLowerD=p.N("PointLowerD",120),PointWebD=p.N("PointWebD",60),PointE=p.N("PointE",400),PointTip=p.N("PointTip",1800),PointDepth=p.N("PointDepth",450),PointPanels=p.I("PointPanels",6)};
        var settings=new Settings{HeightMax=p.N("SiteH",16000),SiteWidth=p.N("SiteW",30000),EndMargin=p.N("SiteEnd",2500),AxisLength=p.N("L"),SiteOrigin=p.N("SiteL")>0?p.N("SiteEnd",2500):0,LD=p.N("PointLowerD",120),LT=p.N("PointLowerT",10),WD=p.N("PointWebD",60),WT=p.N("PointWebT",6),AD=p.N("ArchD"),AT=p.N("ArchT"),PD=p.N("PurlinD"),PT=p.N("PurlinT"),E=p.N("E"),Nu=p.N("Nu"),Rho=p.N("Density"),G=p.N("G"),Fc=p.N("Fc"),Ft=p.N("Ft"),Fb=p.N("Fb"),RoofG=p.N("RoofG"),RoofQ=p.N("RoofQ"),W0=p.N("W0"),MuZ=p.N("MuZ",1),Beta=p.N("Beta",1),Cp=p.N("Cp",.8),Cs=p.N("Cs",-1),WindModel=p.I("WindModel"),WindDirection=p.I("WindDirection"),WindEnds=p.I("WindEnds"),WindReverse=p.B("WindReverse"),WindAngle=p.N("WindAngle"),WindRoofWindward=p.N("WindRoofWindward",.8),WindRoofLeeward=p.N("WindRoofLeeward",-.5),WindRoofParallel=p.N("WindRoofParallel",-.7),WindEndWindward=p.N("WindEndWindward",.8),WindEndLeeward=p.N("WindEndLeeward",-.5),WindEndParallel=p.N("WindEndParallel",-.7),Snow=p.N("SnowQ"),Support=p.I("Support"),Joints=p.I("Joints"),Mesh=p.I("Mesh",2),Limit=p.N("Limit")>0?p.N("Limit"):spec.W/250,Pmax=p.N("Pmax"),Buckle=p.B("Buckle")};settings.Validate();
        CandidateSpace space=null;List<double> factors=null,diameters=null;
        var types=new List<int>{0,1,2,4,5};int maximum=p.I("MaxCandidates",300),requested=0;
        if(!p.Single){
            space=CandidateSpace.Create(p.List("CrossAngles",new[]{25.0,15,35}),p.List("TrussDepths",new[]{450.0,300,600}),p.List("PanelCounts",new[]{8.0,6,10}),p.List("PointEs",new[]{1200.0}),p.List("PointSlopes",new[]{.9}),p.List("PointFootGaps",new[]{10.0}),p.List("PointPanelCounts",new[]{6.0}));
            spec=ComparisonRules.WithType(space.Baseline(spec),0);
            factors=p.B("HeightSearch",true)?ComparisonRules.Factors(p.N("Hmin",.9),p.N("Hmax",1.1),p.I("Samples",3)):new List<double>{1};
            diameters=p.List("Diameters",new[]{settings.AD,150,300}).Distinct().ToList();
            Settings.Require(diameters.Count>0&&diameters.Count<=6&&diameters.All(d=>double.IsFinite(d)&&d>=40&&d<=400&&d>2*settings.AT),"外径候选须为40—400mm且大于两倍壁厚，最多6个");
            requested=space.CheckBudget(types,factors.Count,diameters.Count,maximum);
        }
        StructureRules.Validate(spec,.001);
        var stations=MathRules.Stations(p.N("L"),p.N("S"),p.B("End",true),.001);Settings.Require(stations.Count>=2&&stations.Count<=48,"真实分析要求2—48榀");
        Settings.Require(spec.Structure!=1||(!p.BValue("Axis")&&!p.BValue("HeightMode")),"交叉拱仅支持直轴、等高");
        var planes=new List<Plane>();var heights=new List<double>();var profiles=new List<Profile>();
        foreach(var d in stations){double angle=p.I("Axis")==0?0:p.N("Bend",16)*Math.PI/180*d/p.N("L"),radius=p.I("Axis")==0?0:p.N("L")/(p.N("Bend",16)*Math.PI/180);var o=new P3(radius*(1-Math.Cos(angle)),(p.I("Axis")==0?d:radius*Math.Sin(angle))+settings.SiteOrigin,0);planes.Add(new Plane(new Point3d(o.X,o.Y,o.Z),new Vector3d(Math.Cos(angle),-Math.Sin(angle),0),Vector3d.ZAxis));double h=spec.H+(p.I("HeightMode")==1?p.N("Amplitude",240)*Math.Sin(Math.PI*2*d/p.N("L")):0);heights.Add(h);var sp=spec.Copy();sp.H=h;StructureRules.Validate(sp,.001);profiles.Add(StructureRules.PhysicalProfile(sp,.001));}
        int rails=MathRules.RailCount(profiles,p.I("PMode"),p.N("P"),p.I("PurlinCount"));Settings.Require(rails<=61,"檩条超过61道");
        var g=Engine.Geometry(spec,planes,heights,rails,1,.001);
        void Check(){if(job.Cancel)throw new OperationCanceledException();}
        settings.Check=Check;Check();job.Message=p.Single?"正在复核已选方案":"正在计算共同模数基准";var current=Engine.Evaluate("C 当前设计",g,settings);job.Done=1;
        if(p.Single){job.Total=job.Done=1;return new{schema="bamboo-local-result/v1",mode="single",engineId="karamba3d",engine="Karamba3D",engineVersion=typeof(KarambaCommon.Toolkit).Assembly.GetName().Version.ToString(),host="Rhino 8 / BambooKarambaBridge 0.8.0",createdAt=DateTime.UtcNow,fingerprint=p.Fingerprint,parameters=p.Parameters,C=Pack(current,spec,p),scope=Scope,windScope=WindLoads.Scope,roofScope="分析采用预设随拱荷载面；第六步屋面只控制外观。",search=new{executed=false,evaluated=1}};}
        // Use exactly the GH v0.8.0 comparison engine and candidate generator.
        // Total counts unique planned solves, including independent baselines; cache hits do not solve again.
        var planned=space.Seeds(spec,types,factors,diameters).Select(x=>x.Id).ToHashSet(StringComparer.Ordinal);
        foreach(var type in types)planned.Add(CandidateSpace.Key(ComparisonRules.WithType(space.Baseline(spec),type),1,settings.AD));
        job.Total=planned.Count;
        var result=ComparisonEngine.Search(spec,planes,heights,rails,settings,types,factors,diameters,.001,p.N("AreaTol",.15),current,space,maximum,(name,cg,cs)=>{
            Check();job.Message="Karamba3D："+name+" / 高度×"+cg.Factor.ToString("0.##")+" / 外径 "+cs.AD+" / "+CandidateSpace.Parameters(cg.Network.Spec);
            var e=Engine.Evaluate(name,cg,cs);job.Done++;Check();return e;
        });
        Check();var best=result.Best;var baselineRows=result.Baselines;
        // Invalid geometry and cache reuse can skip FE calls; completion is the whole planned search.
        if(result.Complete)job.Done=job.Total;
        var improvement=result.Complete?result.Representatives.Values.Where(x=>x!=null&&x.Metric.LocallyEligible).OrderBy(x=>x.Metric.Pass?0:1).ThenBy(x=>x.Metric.Pass?x.Metric.Mass:x.Metric.Score).ThenBy(x=>x.Metric.Pass?x.Metric.Score:x.Metric.Mass).ThenBy(x=>x.Metric.Type).FirstOrDefault():null;
        string recommendation=ComparisonEngine.Recommendation(result,current);
        return new {schema="bamboo-local-result/v1",engineId="karamba3d",engine="Karamba3D",engineVersion=typeof(KarambaCommon.Toolkit).Assembly.GetName().Version.ToString(),host="Rhino 8 / BambooKarambaBridge 0.8.0",createdAt=DateTime.UtcNow,fingerprint=p.Fingerprint,parameters=p.Parameters,
            baseline=baselineRows.Select(x=>new{type=x.Metric.Type,eligible=x.Metric.Eligible,reason=x.Metric.Reason,result=Pack(x.Eval,x.Spec,p)}).ToArray(),
            types=new[]{0,1,2,4,5}.Select(t=>{var rep=result.Representatives.GetValueOrDefault(t);bool feasible=result.Complete&&rep!=null&&rep.Metric.LocallyEligible&&rep.Metric.Pass;return new{type=t,feasible,status=!result.Complete?"搜索未完成":feasible?"本型最优（当前搜索范围）":rep?.Eval.Solved==true?"已计算，未通过初筛":rep?.Eval.Attempted==true?"求解未完成":"未计算，条件不满足",diagnostic=new{attempted=rep?.Eval.Attempted??false,solved=rep?.Eval.Solved??false,state=rep?.Eval.State??result.StopReason,error=rep?.Eval.Error??"",warnings=rep?.Eval.Warnings??new List<string>(),globallyComparable=rep?.Metric.Allowed??false,candidateCount=result.Candidates.Count(x=>x.Metric.Type==t),solvedCount=result.Candidates.Count(x=>x.Metric.Type==t&&x.Eval.Solved)},reason=ComparisonEngine.TypeReport(result,t),result=feasible?Pack(rep.Eval,rep.Spec,p):null,reference=rep?.Eval.Solved==true?Pack(rep.Eval,rep.Spec,p):null};}).ToArray(),
            relativeBest=best==null&&improvement!=null?Pack(improvement.Eval,improvement.Spec,p):null,
            C=Pack(current,spec,p),B=best==null?null:Pack(best.Eval,best.Spec,p),recommendation,scope=Scope,windScope=WindLoads.Scope,roofScope="分析采用预设随拱荷载面；第六步屋面只控制外观，不改变本次荷载或结构结果。展示屋面的面积不等于分析荷载面积。",
            materialScope="E=10850MPa、Nu=0.4、Density=644kg/m³为此前资料初值，文献[17][18]仍待核实。G=0时按等效各向同性推导，非实测。所有演示初值都可改。",
            candidates=result.Candidates.Select(x=>new{id=x.Metric.Id,type=x.Metric.Type,factor=x.Metric.Factor,diameter=x.Metric.Diameter,parameters=CandidateParameters(x.Spec),eligible=x.Metric.Eligible,pass=x.Metric.Pass,reason=x.Metric.Reason,massKg=Finite(x.Eval.Solved?x.Metric.Mass:double.NaN),displacementMm=Finite(x.Eval.Solved?x.Metric.Displacement:double.NaN)}).ToArray(),
            search=new{complete=result.Complete,requested,maximum,heightSearch=p.B("HeightSearch",true),plan=space.Plan(types,factors.Count,diameters.Count),byType=types.Select(t=>new{type=t,count=space.Variants(t)*factors.Count*diameters.Count}).ToArray(),baselineUsesFirstValues=true,roofAreaTolerance=p.N("AreaTol",.15),heightMin=p.N("Hmin",.9),heightMax=p.N("Hmax",1.1),samples=p.I("Samples",3),diameters=diameters.ToArray()}};
    }
    static object CandidateParameters(Spec s)=>new{CrossAngle=s.CrossAngle,TrussDepth=s.Depth,Panels=s.Panels,PointE=s.PointE,PointSlope=s.PointSlope,PointFootGap=s.PointFootGap,PointPanels=s.PointPanels};
    static bool BValue(this Input p,string key)=>p.N(key)!=0;
    static double? Finite(double v)=>double.IsFinite(v)?v:null;
    static object Point(Point3d p)=>new{x=p.X,y=p.Y,z=p.Z};
    static object Point(P3 p)=>new{x=p.X,y=p.Y,z=p.Z};
    static object Pack(Evaluation e,Spec spec,Input input){
        var parameters=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(input.Parameters.GetRawText());
        void Set(string name,object value)=>parameters[name]=JsonSerializer.SerializeToElement(value);
        double factor=e.Geometry?.Factor??1;Set("type",ComparisonRules.Code(spec));Set("H",e.Geometry?.VerticalHeights[0]??spec.LegH+(input.N("H")-spec.LegH)*factor);Set("CrossAngle",spec.CrossAngle);Set("TrussDepth",spec.Depth);Set("Panels",spec.Panels);Set("PointE",spec.PointE);Set("PointSlope",spec.PointSlope);Set("PointFootGap",spec.PointFootGap);Set("PointPanels",spec.PointPanels);Set("Amplitude",input.N("Amplitude",240)*factor);Set("ArchD",e.Settings.AD);Set("PMode",1);Set("PurlinCount",e.Geometry?.Grid[0].Length??input.I("PurlinCount"));
        // Network grid has extra intersection/panel columns, NOT the purlin count.
        if(e.Geometry?.Network!=null)Set("PurlinCount",e.Geometry.Network.RailCount);
        var areas=e.Geometry==null?new[]{double.NaN,double.NaN}:ComparisonEngine.Areas(e.Geometry);
        var roofTriangles=e.Geometry?.Network.Roof.Select(t=>new[]{Point(t.A),Point(t.B),Point(t.C)}).ToArray();
        return new {name=e.Name,type=ComparisonRules.Code(spec),factor,parameters,state=e.State,solved=e.Solved,pass=e.Pass,error=e.Error,warnings=e.Warnings,
            screeningScore=Finite(e.Solved?e.Score:double.NaN),displacementMm=Finite(e.Solved?e.Displacement:double.NaN),stressMPa=Finite(e.Solved?e.StressMax:double.NaN),massKg=Finite(e.Solved?e.Mass:double.NaN),stressRatio=Finite(e.Solved?e.Ratio:double.NaN),bucklingFactor=Finite(e.Solved?e.Buckling:double.NaN),equilibriumError=Finite(e.Solved?e.Equilibrium:double.NaN),momentEquilibriumError=(double?)null,equationResidual=(double?)null,limitMm=e.Settings.Limit,
            stressMember=e.StressMember,stressCase=e.StressCase,displacementCase=e.DisplacementCase,planAreaM2=Finite(areas[0]),surfaceAreaM2=Finite(areas[1]),cases=e.CaseReports,roofTriangles,wind=new{model=e.Settings.WindModel,ends=e.Settings.WindEnds,basePressure=e.Settings.W0*e.Settings.MuZ*e.Settings.Beta,scope=WindLoads.Scope,cases=(e.Geometry?.Network!=null?WindLoads.Build(e.Geometry.Network,e.Settings).Select(WindLoads.Pack).ToArray():Array.Empty<object>())},
            members=e.Solved?e.Members.Select((m,i)=>new{id=m.Id,a=Point(m.A),b=Point(m.B),role=m.Role=="Purlin"?"purlin":m.Role=="Web"?"web":"arch",diameterMm=m.Diameter,stressMPa=e.Stresses[i]}).ToArray():null};
    }
}
