﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ComputerInfo.aspx.cs" Inherits="ITSWebMgmt.ComputerInfo" MasterPageFile="~/Site.Master" %>
<%@ Register TagPrefix="uc" TagName="Changes" Src="~/Controls/ActiveChanges.ascx" %>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <script>
        $(document).ready(function () {
            $('.menu .item').tab({ history: false });
        });
    </script>

    <div id="loader">
        <div class="ui active dimmer" style="display: none">
            <div class="ui text loader">Loading</div>
        </div>
    </div>

    <uc:Changes id="changes" runat="server" />

    <h1>Computer Info</h1>
    <div>
        
        
        
        <form method="get">
            Computername: 
        <div class="ui action input">

            <input name="computername" class="ui input focus" value="ITS\AAU804396" />

            <input type="submit" value="Search" onclick='$("#loader > div").show("fast");'>
        </div>
        </form>


        <br />
    </div>

    <div id="content">
        <form runat="server">
            <h2>
                <asp:Label ID="UserNameLabel" runat="server"></asp:Label></h2>


            <asp:Label ID="ResultLabel" runat="server"></asp:Label>

            <div runat="server" id="ResultDiv">

                <div class="ui grid">
                    <div class="four wide column">
                        <div class="ui vertical fluid tabular menu">
                            <a class="active item" data-tab="basicinfo">Basic Info</a>
                            <!--<a class="item" data-tab="userInformation">User Information</a>-->
                            <!--<a class="item" data-tab="advancedinfo">Advanced Info</a> -->
                            <a class="item" data-tab="groups">Groups</a>
                            <a class="item" data-tab="sccmInfo">SCCM Info</a>
                            <a class="item" data-tab="sccmInventory">Inventory</a>
                            <!--<a class="item" data-tab="networkdrives">Networkdrives</a>-->
                            <a class="item" data-tab="rawdata">Raw Data</a>
                            <a class="item" data-tab="tasks">Tasks</a>
                            <!--                    <a class="item" data-tab="statustest">Statustest</a> -->

                        </div>
                    </div>
                    <div class="twelve wide stretched column">
                        <div class="ui tab segment">
                            none<!-- spacer as the fist elemen else is placed differencet -->
                        </div>
                        <div class="ui active tab segment" data-tab="basicinfo">


                            <table class="ui definition table">
                                <tbody>
                                    <tr>
                                        <td>Domain</td>
                                        <td>
                                            <asp:Label runat="server" ID="labelDomain" /></td>
                                    </tr>
                                    <tr>
                                        <td>Admin Pwd Expire:</td>
                                        <td>
                                            <asp:Label runat="server" ID="labelPwdExpireDate" /></td>
                                    </tr>
                                    <tr>
                                        <td>PC Config:</td>
                                        <td>
                                            <asp:Label runat="server" ID="labelBasicInfoPCConfig" /></td>
                                    </tr>
                                    <tr>
                                        <td>Bitlocker Enabled:</td>
                                        <td>
                                            <asp:Label runat="server" ID="labelBasicInfoExtraConfig" /></td>
                                    </tr>
                                    <tr>
                                        <td>Modtager test updates:</td>
                                        <td>
                                            <asp:Label runat="server" ID="labelBasicInfoTestUpdates" /></td>
                                    </tr>
                                </tbody>
                            </table>
                            <br />
                            <asp:Button ID="ResultGetPassword2" runat="server" value="" Text="Get Local Admin Password" OnClick="ResultGetPassword_Click" CssClass="ui button" />
                        </div>




                        <div class="ui tab segment" data-tab="userInformation">
                        </div>
                        <div class="ui tab segment" data-tab="rawdata">
                            <h2>Raw AD data</h2>
                            <asp:Label ID="ResultLabelRaw" runat="server"></asp:Label>
                        </div>
                        <div class="ui tab segment" data-tab="tasks">
                            <h2>Tasks</h2>
                            <br />
                            <asp:Button ID="ResultGetPassword" runat="server" value="" Text="Get Local Admin Password" OnClick="ResultGetPassword_Click" CssClass="ui button" />
                            <br />

                            <asp:Button ID="buttonEnableBitlockerEncryption" runat="server" value="" Text="Enable Bitlocker Encryption" OnClick="buttonEnableBitlockerEncryption_Click" CssClass="ui button" />
                            <br />
                            <div id="MoveComputerOUdiv" runat="server">
                                <asp:Button ID="MoveComputerOU" runat="server" value="" Text="Move computer to OU Clients" OnClick="MoveOU_Click" CssClass="ui button" />
                                Only do this if you know what you are doing!
                            </div>



                        </div>
                        <div class="ui tab segment" data-tab="sccmInfo">
                            <h2>SCCM Info</h2>
                            <h3>Collections</h3>
                            <asp:Label runat="server" ID="labelSCCMCollections" />
                        </div>

                        <div class="ui tab segment" data-tab="sccmInventory">
                            <h2>SCCM Info</h2>
                            <h3>Collections</h3>
                            <asp:Label runat="server" ID="labelSCCMInventory" />
                        </div>
                        

                        <div class="ui tab segment" data-tab="groups">
                            <h2>Groups</h2>
                            <asp:Label runat="server" ID="groupssegmentLabel" />
                        </div>
                    </div>
                </div>

            </div>
        </form>
    </div>
</asp:Content>
