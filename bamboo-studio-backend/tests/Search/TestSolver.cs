// Deterministic test double for FE evaluation. Geometry uses the unchanged real kernel.
// These mock metrics are never delivered as engineering results.
using System;
using System.Linq;
using System.Collections.Generic;
using BambooStages123;
using BambooCompare;
namespace Bamboo45 {
 public class Settings {
  public double AD=100,HeightMax=6500,SiteWidth=8000,EndMargin=2500,AxisLength=12000,SiteOrigin=2500;
  public bool Strength;
  public Settings Copy()=>(Settings)MemberwiseClone();
  public static void Require(bool b,string msg){if(!b)throw new ArgumentException(msg);}
 }
 public class GeometrySet {public StructurePlan Network;public List<double> VerticalHeights;}
 public class Evaluation {
  public string Name,State="mock",Error="";public GeometrySet Geometry;public Settings Settings;
  public bool Solved,Pass;public double Score,Mass,StressMax,Displacement;
  public List<string> Warnings=new List<string>();
 }
 public static class Engine {
  public static List<string> Calls=new List<string>();
  public static GeometrySet Geometry(Spec s,List<Frame3> frames,List<double> heights,int rails,double factor,double tolerance){
   var hs=heights.Select(h=>s.LegH+(h-s.LegH)*factor).ToList();return new GeometrySet{Network=StructureRules.Build(s,frames,hs,rails,tolerance),VerticalHeights=hs};
  }
  public static Evaluation Evaluate(string name,GeometrySet g,Settings s){
   var sp=g.Network.Spec;double factor=(g.VerticalHeights[0]-sp.LegH)/(4040-sp.LegH);
   Calls.Add(CandidateSpace.Key(sp,factor,s.AD));
   bool pass=!(sp.Structure==2&&!sp.CompoundPointed&&sp.Depth==450&&sp.Panels==8);
   return new Evaluation{Name=name,Geometry=g,Settings=s,Solved=true,Pass=pass,Score=pass?.5:1.2,Mass=1000+s.AD+sp.Depth*.01+sp.Panels,StressMax=3,Displacement=8};
  }
 }
}
