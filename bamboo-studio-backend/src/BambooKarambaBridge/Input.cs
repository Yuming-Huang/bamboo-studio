using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BambooStudio;
public sealed class Input {
    public JsonElement Parameters;
    public string Fingerprint;
    public bool Single;
    public double N(string key,double fallback=0)=>Parameters.TryGetProperty(key,out var v)?v.GetDouble():fallback;
    public List<double> List(string key,IEnumerable<double> fallback)=>Parameters.TryGetProperty(key,out var v)?v.EnumerateArray().Select(x=>x.GetDouble()).ToList():fallback.ToList();
    public int I(string key,int fallback=0)=>(int)N(key,fallback);
    public bool B(string key,bool fallback=false)=>Parameters.TryGetProperty(key,out var v)?v.GetBoolean():fallback;
    public static Input Parse(string json){
        using var d=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=8});
        if(!d.RootElement.TryGetProperty("schema",out var s)||s.GetString()!="bamboo-local/v1")throw new ArgumentException("接口版本不匹配");
        var p=d.RootElement.GetProperty("parameters");if(p.ValueKind!=JsonValueKind.Object)throw new ArgumentException("缺少参数对象");
        string mode=d.RootElement.TryGetProperty("mode",out var modeValue)?modeValue.GetString():"compare";
        if(mode!="compare"&&mode!="single")throw new ArgumentException("未知计算模式");
        var input=new Input{Parameters=p.Clone(),Single=mode=="single",Fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(p.GetRawText())))};
        var bools=new HashSet<string>{"End","Run","Optimize","Buckle","CrossCompare","WindReverse","HeightSearch"};
        var ranges=new Dictionary<string,(double lo,double hi,bool integer)> {
            ["SideReserve"]=(0,15000,false),["MaxCandidates"]=(1,2000,true),["PointedVersion"]=(0,3,true),["PointFootGap"]=(0,500,false),["PointSlope"]=(.35,.95,false),["PointE"]=(50,2000,false),["PointTip"]=(100,6000,false),["PointDepth"]=(50,2000,false),["PointPanels"]=(3,12,true),["PointLowerD"]=(40,400,false),["PointLowerT"]=(1,80,false),["PointWebD"]=(30,300,false),["PointWebT"]=(1,60,false),
            ["SiteL"]=(0,42000,false),["SiteEnd"]=(0,6000,false),["AreaTol"]=(0,1,false),["SiteW"]=(2000,30000,false),["SiteH"]=(1500,16000,false),["SearchD2"]=(40,400,false),["SearchD3"]=(40,400,false),["type"]=(0,5,true),["Foot"]=(0,2,true),["W"]=(2000,12000,false),["H"]=(500,12000,false),["LegH"]=(0,6000,false),["LegAngle"]=(0,40,false),["CrossAngle"]=(5,45,false),["TrussDepth"]=(100,1500,false),["Panels"]=(4,24,true),
            ["L"]=(2000,42000,false),["S"]=(500,6000,false),["Axis"]=(0,1,true),["Bend"]=(1,60,false),["HeightMode"]=(0,1,true),["Amplitude"]=(0,1500,false),["PMode"]=(0,1,true),["P"]=(200,2500,false),["PurlinCount"]=(3,61,true),
            ["RoofType"]=(0,2,true),["SideEave"]=(0,3000,false),["EndEave"]=(0,3000,false),["RoofOffset"]=(0,3000,false),["Facets"]=(2,24,true),["RidgeWidth"]=(300,3000,false),["RidgeRise"]=(100,1500,false),
            ["ArchD"]=(40,400,false),["ArchT"]=(1,80,false),["PurlinD"]=(40,300,false),["PurlinT"]=(1,60,false),["E"]=(1000,30000,false),["Nu"]=(0,.49,false),["Density"]=(100,1500,false),["G"]=(0,30000,false),["Fc"]=(0,300,false),["Ft"]=(0,300,false),["Fb"]=(0,300,false),
            ["WindModel"]=(0,1,true),["WindDirection"]=(0,4,true),["WindEnds"]=(0,1,true),["WindAngle"]=(0,360,false),["WindRoofWindward"]=(-5,5,false),["WindRoofLeeward"]=(-5,5,false),["WindRoofParallel"]=(-5,5,false),["WindEndWindward"]=(-5,5,false),["WindEndLeeward"]=(-5,5,false),["WindEndParallel"]=(-5,5,false),
            ["RoofG"]=(0,3,false),["RoofQ"]=(0,3,false),["W0"]=(0,3,false),["MuZ"]=(.1,5,false),["Beta"]=(.1,5,false),["Cp"]=(0,5,false),["Cs"]=(-5,0,false),["SnowQ"]=(0,3,false),["Support"]=(0,1,true),["Joints"]=(0,3,true),["Mesh"]=(1,4,true),["Limit"]=(0,1000,false),["Pmax"]=(0,3000,false),["Hmin"]=(.5,1.5,false),["Hmax"]=(.5,1.5,false),["Samples"]=(2,9,true)
        };
        var names=new HashSet<string>();foreach(var a in p.EnumerateObject()){
            if(!names.Add(a.Name))throw new ArgumentException("重复参数："+a.Name);
            if(new[]{"Diameters","CrossAngles","TrussDepths","PanelCounts","PointEs","PointSlopes","PointFootGaps","PointPanelCounts"}.Contains(a.Name)){if(a.Value.ValueKind!=JsonValueKind.Array||a.Value.GetArrayLength()<1||a.Value.GetArrayLength()>12||a.Value.EnumerateArray().Any(v=>v.ValueKind!=JsonValueKind.Number||!v.TryGetDouble(out var n)||!double.IsFinite(n)))throw new ArgumentException(a.Name+"须为有效数值候选列表");continue;}
            if(bools.Contains(a.Name)){if(a.Value.ValueKind!=JsonValueKind.True&&a.Value.ValueKind!=JsonValueKind.False)throw new ArgumentException(a.Name+"须为布尔值");continue;}
            if(!ranges.TryGetValue(a.Name,out var r))throw new ArgumentException("未知参数："+a.Name);
            if(a.Value.ValueKind!=JsonValueKind.Number||!a.Value.TryGetDouble(out double n)||!double.IsFinite(n)||n<r.lo||n>r.hi||(r.integer&&n!=Math.Truncate(n)))throw new ArgumentException(a.Name+"超出允许范围");
        }
        foreach(var key in new[]{"type","Foot","W","H","LegH","LegAngle","L","S","PMode","P","PurlinCount","ArchD","ArchT","PurlinD","PurlinT","E","Nu","Density","RoofG","RoofQ","W0","RoofType","SideEave","EndEave","RoofOffset"})if(!names.Contains(key))throw new ArgumentException("缺少参数："+key);
        if(!input.B("Run"))throw new ArgumentException("请先开启Run");
        if(input.I("Panels",8)%2!=0||input.I("PurlinCount")%2!=1)throw new ArgumentException("桁架分格须偶数；檩条道数须奇数");
        if(input.B("HeightSearch",true)&&input.N("Hmin",.9)>input.N("Hmax",1.1))throw new ArgumentException("搜索高度下限不得大于上限");
        if(input.N("SiteL")>0&&(input.N("SiteL")<2000||Math.Abs(input.N("L")-(input.N("SiteL")-2*input.N("SiteEnd",2500)))>.001))throw new ArgumentException("阵列可用长度必须等于场地长度减去两端预留");
        if(input.N("S")>input.N("L"))throw new ArgumentException("榀距不得超过扣除两端预留后的阵列可用长度");
        if(input.I("Axis")==1&&input.N("L")/(input.N("Bend",16)*Math.PI/180)<=input.N("W")/2)throw new ArgumentException("曲轴弯曲过急，内侧阵列跨度线重叠");
        if(input.N("W")>input.N("SiteW",30000)||input.N("H")>input.N("SiteH",16000))throw new ArgumentException("跨度或总高超过场地边界");
        if(input.I("type")==3)throw new ArgumentException("新版五型不包含交叉尖拱");
        if(!input.Single){
            var side=input.N("SideReserve",1200);if(side*2>=input.N("SiteW",30000)||input.N("W")>input.N("SiteW",30000)-2*side+.001)throw new ArgumentException("共同跨度超出场地及每侧布局预留");
        }
        return input;
    }
}
