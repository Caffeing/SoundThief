using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoundThief.JinHo.Tests
{
    public sealed class PropagationTests
    {
        Scene scene;
        JinHoSoundPropagation policy;
        [SetUp] public void SetUp()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var obj = new GameObject("Acoustic test"); SceneManager.MoveGameObjectToScene(obj,scene);
            policy=obj.AddComponent<JinHoSoundPropagation>();
            policy.regions=new[] { Room("A",0,4,0,4), Room("B",4,8,0,4), Room("C",8,12,0,4) };
            policy.connections=new[] {
                new AcousticConnection { name="AB",regionA=0,regionB=1,point=new Vector3(4,1,2),wall=true },
                new AcousticConnection { name="BC",regionA=1,regionB=2,point=new Vector3(8,1,2),wall=true }
            };
        }
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(scene);
        static AcousticRegion Room(string name,float x1,float x2,float z1,float z2)
            => new AcousticRegion { name=name,min=new Vector2(x1,z1),max=new Vector2(x2,z2) };
        [Test] public void SameRoomKeepsDirectDistanceAndFullGain()
        {
            var a=new Vector3(1,1,1); var b=new Vector3(3,1,1);
            var path=policy.Evaluate(a,b);
            Assert.That(path.valid); Assert.That(path.distance,Is.EqualTo(2).Within(.001f));
            Assert.That(path.closedBoundaries,Is.Zero);
            Assert.That(policy.AudioGain(a,2,null,b),Is.EqualTo(1));
        }
        [Test] public void TwoWallsAccumulateCostAndNonzeroSignal()
        {
            var a=new Vector3(2,1,2); var b=new Vector3(10,1,2);
            var path=policy.Evaluate(a,b);
            Assert.That(path.distance,Is.EqualTo(8).Within(.001f));
            Assert.That(path.closedBoundaries,Is.EqualTo(2));
            Assert.That(path.cost,Is.EqualTo(32).Within(.001f));
            Assert.That(policy.AudioGain(a,2,null,b),Is.EqualTo(.0625f).Within(.0001f));
        }
        [Test] public void OpenDetourCanBeatTransmissionThroughWall()
        {
            policy.regions=new[] { Room("A",0,4,0,4),Room("B",4,8,0,4),Room("Bridge",0,8,4,6) };
            policy.connections=new[] {
                new AcousticConnection { name="wall",regionA=0,regionB=1,point=new Vector3(4,1,2),wall=true },
                new AcousticConnection { name="left opening",regionA=0,regionB=2,point=new Vector3(2,1,4) },
                new AcousticConnection { name="right opening",regionA=2,regionB=1,point=new Vector3(6,1,4) }
            };
            var path=policy.Evaluate(new Vector3(2,1,2),new Vector3(6,1,2));
            Assert.That(path.closedBoundaries,Is.Zero);
            Assert.That(path.distance,Is.EqualTo(8).Within(.001f));
            Assert.That(policy.AudioGain(new Vector3(2,1,2),2,null,new Vector3(6,1,2)),Is.EqualTo(.5f).Within(.001f));
        }
        [Test] public void OutsideGraphIsInvalidRatherThanUnattenuated()
            => Assert.That(policy.Evaluate(new Vector3(-5,1,2),new Vector3(6,1,2)).valid,Is.False);
        [TestCase(0,false)] [TestCase(2,true)]
        public void OpenRouteAllowanceKeepsTheOriginalDirectRadius(float extra,bool expected)
        {
            policy.regions=new[]{Room("A",0,4,0,4),Room("B",4,8,0,4),Room("Bridge",0,8,4,6)};
            policy.connections=new[]{new AcousticConnection{regionA=0,regionB=2,point=new Vector3(2,1,4)},new AcousticConnection{regionA=1,regionB=2,point=new Vector3(6,1,4)}};
            var actor=new GameObject("Receiver");SceneManager.MoveGameObjectToScene(actor,scene);
            var guard=actor.AddComponent<GuardAgent>();var world=actor.AddComponent<SoundWorld>();var rules=ScriptableObject.CreateInstance<SoundRules>();world.rules=rules;guard.world=world;
            try
            {
                guard.transform.position=new Vector3(6,1,2);policy.openRouteExtraDistance=extra;
                Assert.That(policy.AllowsHearing(new Vector3(2,1,2),1,null,guard),Is.EqualTo(expected));
                policy.openRouteExtraDistance=100;guard.transform.position=new Vector3(8,1,2);
                Assert.That(policy.AllowsHearing(new Vector3(1,1,2),1,null,guard),Is.False,"Open detour allowance must not enlarge the original Euclidean radius.");
                guard.transform.position=new Vector3(6,1,2);policy.connections[0].wall=true;
                Assert.That(policy.AllowsHearing(new Vector3(2,1,2),1,null,guard),Is.False,"The extra budget must not apply to a closed boundary.");
            }
            finally {Object.DestroyImmediate(actor);Object.DestroyImmediate(rules);}
        }
        [Test] public void CrossingOutsideEitherRoomFailsValidation()
        {
            policy.connections[0].point=new Vector3(20,1,2);
            Assert.That(policy.Validate(),Is.Not.Empty);
        }
        [Test] public void BoundarySourceSeedsBothSidesWithoutDoorSpecificExceptions()
        {
            var path=policy.Evaluate(new Vector3(4,1,2),new Vector3(6,1,2));
            Assert.That(path.closedBoundaries,Is.Zero);
            Assert.That(path.distance,Is.EqualTo(2).Within(.001f));
        }
        [Test] public void OverlappingRoomsCannotBypassTheirBoundary()
        {
            policy.regions[1].min.x=3;
            Assert.That(policy.Validate(),Is.Not.Empty);
        }
        [Test] public void DifferentOwnerCannotReplaceOrRemoveRegisteredPolicy()
        {
            var world=policy.gameObject.AddComponent<SoundWorld>();
            var other=policy.gameObject.AddComponent<JinHoSoundPropagation>();
            Assert.That(world.RegisterPropagationPolicy(policy));
            Assert.That(world.RegisterPropagationPolicy(other),Is.False);
            Assert.That(world.RegisterPropagationPolicy(world),Is.False);
            world.UnregisterPropagationPolicy(other);
            Assert.That(world.PropagationPolicy,Is.SameAs(policy));
            world.UnregisterPropagationPolicy(policy);
            Assert.That(world.PropagationPolicy,Is.Null);
        }
    }
}
