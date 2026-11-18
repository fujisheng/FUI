using System.IO;
using NUnit.Framework;
namespace FUI.Portable.Tests
{
    public sealed class GeneratedListBindingTests
    {
        // 若只存VM数据而不运行行Projection，该测试的元素初值与点击状态均不正确。
        [Test]
        public void InitialRowsUseGeneratedItemTextAndCommand()
        {
            var result=Run(@"
                var vm=new ListVm(); vm.Items.Add(new RowVm(""a"")); vm.Items.Add(new RowVm(""b""));
                var view=CreateList(out var list);
                using(var projection=Projection.Create(view,vm))
                {
                    projection.Activate();
                    var initial=Texts(list);
                    vm.Items[0].Label=""updated"";
                    var updated=Texts(list);
                    ((HeadlessClickElement)list.Rows[0].View.GetElement<IClickElement>(""Button"")).Input();
                    return new[] { initial,updated,Texts(list),vm.Items[0].ClickCount.ToString() };
                }");
            Assert.That(result,Is.EqualTo(new[] { "a/b", "updated/b", "picked:updated/b", "1" }));
        }
        [Test]
        public void SingleAndBatchMutationsUpdateActualRowState()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); var view=CreateList(out var list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var states=new System.Collections.Generic.List<string>{ Texts(list) };
                    vm.Items.Add(new RowVm(""b"")); states.Add(Texts(list));
                    vm.Items.Insert(0,new RowVm(""a"")); states.Add(Texts(list));
                    var removed=list.Rows[1]; vm.Items[1]=new RowVm(""c""); states.Add(Texts(list));
                    states.Add(removed.View.Destroyed.ToString());
                    vm.Items.RemoveAt(0); states.Add(Texts(list));
                    vm.Items.AddRange(new[]{new RowVm(""d""),new RowVm(""e"")}); states.Add(Texts(list));
                    vm.Items.InsertRange(1,new[]{new RowVm(""x""),new RowVm(""y"")}); states.Add(Texts(list));
                    vm.Items.RemoveRange(1,2); states.Add(Texts(list));
                    vm.Items.RemoveAll(v=>v.Label==""d""); states.Add(Texts(list));
                    vm.Items.Clear(); states.Add(Texts(list));
                    return states.ToArray();
                }"),Is.EqualTo(new[]{"","b","a/b","a/c","True","c","c/d/e","c/x/y/d/e","c/d/e","c/e",""}));
        }

        [Test]
        public void SortingPreservesRowsByIdentityIncludingDuplicateItems()
        {
            Assert.That(Run(@"
                var a=new RowVm(""a""); var b=new RowVm(""b"");
                var vm=new ListVm(); vm.Items.AddRange(new[]{b,a,b});
                var view=CreateList(out var list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var first=list.Rows[0]; var middle=list.Rows[1]; var last=list.Rows[2];
                    vm.Items.Sort((x,y)=>string.CompareOrdinal(x.Label,y.Label));
                    var sorted=Texts(list);
                    var identities=ReferenceEquals(list.Rows[0],middle)+""/""+ReferenceEquals(list.Rows[1],first)+""/""+ReferenceEquals(list.Rows[2],last);
                    b.Label=""shared"";
                    return new[]{sorted,identities,Texts(list),(list.Rows[1]!=list.Rows[2]).ToString()};
                }"),Is.EqualTo(new[]{"a/b/b","True/True/True","a/shared/shared","True"}));
        }

        [Test]
        public void ChangingCollectionDetachesOldSourceAndRemovedItemProjections()
        {
            Assert.That(Run(@"
                var a=new RowVm(""a""); var vm=new ListVm(); vm.Items.Add(a);
                var view=CreateList(out var list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var old=vm.Items; var oldRow=list.Rows[0];
                    vm.Items=new ObservableList<RowVm>(new[]{new RowVm(""new"")});
                    old.Add(new RowVm(""late"")); a.Label=""old-changed"";
                    var changed=Texts(list);
                    ((HeadlessClickElement)oldRow.View.GetElement<IClickElement>(""Button"")).Input();
                    vm.Items=null;
                    return new[]{changed,oldRow.View.Destroyed.ToString(),a.ClickCount.ToString(),list.Rows.Count.ToString()};
                }"),Is.EqualTo(new[]{"new","True","0","0"}));
        }

        [Test]
        public void SharedVmKeepsLiveProjectionIndependentWhenOtherCloses()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); var item=new RowVm(""a""); vm.Items.Add(item);
                var v1=CreateList(out var l1); var v2=CreateList(out var l2);
                var a=Projection.Create(v1,vm);
                using(var b=Projection.Create(v2,vm))
                {
                    a.Activate(); b.Activate(); var old=l1.Rows[0]; a.Dispose();
                    item.Label=""live""; vm.Items.Add(new RowVm(""b""));
                    ((HeadlessClickElement)old.View.GetElement<IClickElement>(""Button"")).Input();
                    var before=item.ClickCount;
                    ((HeadlessClickElement)l2.Rows[0].View.GetElement<IClickElement>(""Button"")).Input();
                    return new[]{l1.Rows.Count.ToString(),old.View.Destroyed.ToString(),Texts(l2),before.ToString(),item.ClickCount.ToString()};
                }"),Is.EqualTo(new[]{"0","True","picked:live/b","0","1"}));
        }

        [Test]
        public void HiddenRowsPauseGeneratedBindingAndRefreshWhenRevealed()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); vm.Items.Add(new RowVm(""a""));
                var view=CreateList(out var list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var row=list.Rows[0];
                    view.Visible=false; vm.Items[0].Label=""hidden"";
                    ((HeadlessClickElement)row.View.GetElement<IClickElement>(""Button"")).Input();
                    var paused=Texts(list); var clicks=vm.Items[0].ClickCount;
                    view.Visible=true;
                    return new[]{paused,clicks.ToString(),Texts(list),ReferenceEquals(row,list.Rows[0]).ToString()};
                }"),Is.EqualTo(new[]{"a","0","hidden","True"}));
        }

        [Test]
        public void FailedNewRowBindingReleasesNewViewsAndPreservesExistingRows()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); var a=new RowVm(""a""); vm.Items.Add(a);
                var created=new System.Collections.Generic.List<HeadlessView>();
                var list=new HeadlessListElement(""Rows"",item=>{
                    var row=((RowVm)item).Label==""bad"" ? new HeadlessView(""Row"",new HeadlessClickElement(""Button"")) : CreateRow(item);
                    created.Add(row); return row;
                });
                var view=new HeadlessView(""List"",list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var original=list.Rows[0]; bool failed=false;
                    try { vm.Items.AddRange(new[]{new RowVm(""c""),new RowVm(""bad"")}); }
                    catch(InvalidOperationException) { failed=true; }
                    var preserved=ReferenceEquals(original,list.Rows[0]);
                    a.Label=""alive"";
                    var state=Texts(list);
                    var cleanup=created.Skip(1).All(v=>v.Destroyed);
                    vm.Items.RemoveAt(2);
                    return new[]{failed.ToString(),preserved.ToString(),state,cleanup.ToString(),Texts(list)};
                }"),Is.EqualTo(new[]{"True","True","alive","True","alive/c"}));
        }

        [Test]
        public void CachedSameVmReopenHasOneCollectionDeliveryAndPausesClosedRows()
        {
            Assert.That(Run(@"
                var provider=new ListProvider(); var navigator=new Navigator(provider,new ListDispatcher());
                navigator.Initialize();
                try
                {
                    var route=Generated.Routes.ListVmView.CreateViewVariant(""CachedList"",new RoutePolicy(cacheMode:CacheMode.KeepAlive));
                    var vm=new ListVm(); vm.Items.Add(new RowVm(""a""));
                    var handle=navigator.OpenWithViewModel(route,vm); var lease=provider.Leases[0]; var list=lease.List;
                    int deliveries=0; list.RowsChanged+=()=>deliveries++;
                    bool frozen=true, oldDead=true, once=true, reused=true;
                    for(int round=0;round<3;round++)
                    {
                        var old=handle; var row=list.Rows[0]; var before=Texts(list);
                        navigator.Close(old); vm.Items[0].Label=""round""+round;
                        ((HeadlessClickElement)row.View.GetElement<IClickElement>(""Button"")).Input();
                        frozen&=Texts(list)==before && vm.Items[0].ClickCount==0;
                        handle=navigator.OpenWithViewModel(route,vm);
                        oldDead&=!navigator.IsAlive(old)&&navigator.IsAlive(handle)&&old!=handle;
                        reused&=ReferenceEquals(row,list.Rows[0]);
                        var count=deliveries; vm.Items.Add(new RowVm(""item""+round)); once&=deliveries-count==1;
                    }
                    var text=Texts(list); navigator.Shutdown();
                    return new[]{frozen.ToString(),oldDead.ToString(),once.ToString(),reused.ToString(),text,provider.Leases.Count.ToString(),lease.DisposeCount.ToString(),list.Rows.Count.ToString()};
                }
                finally { navigator.Shutdown(); }"),Is.EqualTo(new[]{"True","True","True","True","round2/item0/item1/item2","1","1","0"}));
        }

        [Test]
        public void CachedNewVmRebindDetachesOldCollectionAndOldRows()
        {
            Assert.That(Run(@"
                var provider=new ListProvider(); var navigator=new Navigator(provider,new ListDispatcher());
                navigator.Initialize();
                try
                {
                    var route=Generated.Routes.ListVmView.CreateViewVariant(""CachedList"",new RoutePolicy(cacheMode:CacheMode.KeepAlive));
                    var oldVm=new ListVm(); oldVm.Items.Add(new RowVm(""old""));
                    var h1=navigator.OpenWithViewModel(route,oldVm); var list=provider.Leases[0].List; var oldRow=list.Rows[0];
                    navigator.Close(h1);
                    var newVm=new ListVm(); newVm.Items.Add(new RowVm(""new""));
                    var h2=navigator.OpenWithViewModel(route,newVm); int deliveries=0; list.RowsChanged+=()=>deliveries++;
                    oldVm.Items.Add(new RowVm(""late"")); oldVm.Items[0].Label=""changed"";
                    var oldCount=deliveries;
                    newVm.Items.Add(new RowVm(""current""));
                    return new[]{Texts(list),oldRow.View.Destroyed.ToString(),oldCount.ToString(),deliveries.ToString(),(h1!=h2&&!navigator.IsAlive(h1)).ToString(),provider.Leases.Count.ToString()};
                }
                finally { navigator.Shutdown(); }"),Is.EqualTo(new[]{"new/current","True","0","1","True","1"}));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrWrongListElementFailsRealGeneratedActivation(bool wrongType)
        {
            Assert.That(Run(@"
                var view=" + (wrongType? @"new HeadlessView(""List"",new HeadlessTextElement(""Rows""))" : @"new HeadlessView(""List"")") + @";
                var vm=new ListVm(); var p=Projection.Create(view,vm); string error=""none"";
                try { p.Activate(); } catch(InvalidOperationException e) { error=e.Message; }
                finally { p.Dispose(); }
                return new[]{error,view.Destroyed.ToString()};"),Is.EqualTo(new[]{wrongType?"Element type mismatch: Rows":"Missing required element: Rows","True"}));
        }

        [Test]
        public void ParentInteractionGatePreventsRowCommandDelivery()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); vm.Items.Add(new RowVm(""a"")); var view=CreateList(out var list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); view.Interactable=false;
                    ((HeadlessClickElement)list.Rows[0].View.GetElement<IClickElement>(""Button"")).Input();
                    var blocked=vm.Items[0].ClickCount; view.Interactable=true;
                    ((HeadlessClickElement)list.Rows[0].View.GetElement<IClickElement>(""Button"")).Input();
                    return new[]{blocked.ToString(),vm.Items[0].ClickCount.ToString(),Texts(list)};
                }"),Is.EqualTo(new[]{"0","1","picked:a"}));
        }

        [Test]
        public void RowFactoryReusingAViewFailsAndReleasesNewProjection()
        {
            Assert.That(Run(@"
                var shared=CreateRow(new RowVm());
                var list=new HeadlessListElement(""Rows"",item=>shared);
                var vm=new ListVm(); vm.Items.AddRange(new[]{new RowVm(""a""),new RowVm(""b"")});
                var view=new HeadlessView(""List"",list); var p=Projection.Create(view,vm); bool failed=false;
                try { p.Activate(); } catch(InvalidOperationException) { failed=true; }
                finally { p.Dispose(); }
                return new[]{failed.ToString(),shared.Destroyed.ToString(),list.Rows.Count.ToString()};"),Is.EqualTo(new[]{"True","True","0"}));
        }

        [TestCase("clear")]
        [TestCase("destroy")]
        public void CleanupFailureStillReleasesEveryRow(string operation)
        {
            Assert.That(Run(@"
                var first=new RowVm(""fault""); var second=new RowVm(""b"");
                var vm=new ListVm(); vm.Items.AddRange(new[]{first,second});
                var views=new System.Collections.Generic.List<HeadlessView>();
                var list=new HeadlessListElement(""Rows"",item=>{
                    var v=((RowVm)item).Label==""fault"" ? new HeadlessView(""Row"",new HeadlessTextElement(""Label""),new HeadlessClickElement(""Button""),new CleanupFailureElement()) : CreateRow(item);
                    views.Add(v); return v;
                });
                var view=new HeadlessView(""List"",list); var p=Projection.Create(view,vm); p.Activate();
                string error=""none"";
                try { " + (operation=="clear" ? "vm.Items.Clear();" : "p.Dispose();") + @" }
                catch(Exception e) { error=e.Message; }
                second.Label=""late"";
                ((HeadlessClickElement)views[1].GetElement<IClickElement>(""Button"")).Input();
                p.Dispose();
                return new[]{error,views.All(v=>v.Destroyed).ToString(),second.ClickCount.ToString(),list.Rows.Count.ToString()};"),Is.EqualTo(new[]{"row cleanup failure","True","0","0"}));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BindingFailureRollbackCleansEveryNewRowAndRetainsBothErrors(bool failedRowCleanup)
        {
            Assert.That(Run("var failedRowCleanup="+failedRowCleanup.ToString().ToLowerInvariant()+";"+@"
                var vm=new ListVm(); vm.Items.Add(new RowVm(""a""));
                var views=new System.Collections.Generic.List<HeadlessView>();
                var list=new HeadlessListElement(""Rows"",item=>{
                    var name=((RowVm)item).Label;
                    var v=name==""bad"" ? (failedRowCleanup ? new HeadlessView(""Row"",new HeadlessClickElement(""Button""),new CleanupFailureElement()) : new HeadlessView(""Row"",new HeadlessClickElement(""Button""))) :
                        name==""fault"" ? new HeadlessView(""Row"",new HeadlessTextElement(""Label""),new HeadlessClickElement(""Button""),new CleanupFailureElement()) : CreateRow(item);
                    views.Add(v); return v;
                });
                var view=new HeadlessView(""List"",list);
                using(var p=Projection.Create(view,vm))
                {
                    p.Activate(); var current=new RowVm(""c""); string error=""none"";
                    try { vm.Items.AddRange(new[]{new RowVm(""fault""),current,new RowVm(""bad"")}); }
                    catch(Exception e) { error=e.ToString(); }
                    current.Label=""late"";
                    ((HeadlessClickElement)views[2].GetElement<IClickElement>(""Button"")).Input();
                    vm.Items[0].Label=""alive"";
                    return new[]{error.Contains(""Missing required element: Label"").ToString(),error.Contains(""row cleanup failure"").ToString(),views.Skip(1).All(v=>v.Destroyed).ToString(),current.ClickCount.ToString(),Texts(list)};
                }"),Is.EqualTo(new[]{"True","True","True","0","alive"}));
        }

        [Test]
        public void HiddenSourceReplacementDefersBindingFailureButCloseReleasesEveryOwnedRow()
        {
            Assert.That(Run(@"
                var vm=new ListVm(); vm.Items.Add(new RowVm(""a""));
                var views=new System.Collections.Generic.List<HeadlessView>();
                var list=new HeadlessListElement(""Rows"",item=>{
                    var name=((RowVm)item).Label;
                    var v=name==""bad"" ? new HeadlessView(""Row"",new HeadlessClickElement(""Button"")) :
                        name==""fault"" ? new HeadlessView(""Row"",new HeadlessTextElement(""Label""),new HeadlessClickElement(""Button""),new CleanupFailureElement()) : CreateRow(item);
                    views.Add(v); return v;
                });
                var view=new HeadlessView(""List"",list); var p=Projection.Create(view,vm); p.Activate();
                view.Visible=false;
                vm.Items=new ObservableList<RowVm>(new[]{new RowVm(""fault""),new RowVm(""c""),new RowVm(""bad"")});
                var count=list.Rows.Count; bool failed=false;
                try { view.Visible=true; } catch(InvalidOperationException) { failed=true; }
                var hidden=!view.Visible; string error=""none"";
                try { p.Dispose(); } catch(Exception e) { error=e.Message; }
                return new[]{count.ToString(),failed.ToString(),hidden.ToString(),error,views.All(v=>v.Destroyed).ToString(),list.Rows.Count.ToString()};"),Is.EqualTo(new[]{"3","True","True","row cleanup failure","True","0"}));
        }

        internal static string[] Run(string body)
        {
            var source=File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Tests","HeadlessListScenario.cs.txt")).Replace("/* SCENARIO */",body);
            var assembly=GeneratedCompilation.Compile(("HeadlessListScenario.cs",source));
            return (string[])assembly.GetType("PortableList.Scenario").GetMethod("Run").Invoke(null,null);
        }
    }
}
