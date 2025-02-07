﻿<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebPsdzClient._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <style>.text-left { width: 100%; max-width: 100%; resize: none; }</style>
    <style>.table { border-width: 0; border-color: transparent; }</style>
    <style>.table th { text-align: center }</style>
    <style>.dropdown { width: 100%; max-width: 100%; }</style>
    <style>.checkbox label { text-indent: 30px }</style>
    <div class="jumbotron">
        <asp:UpdatePanel ID="UpdatePanelStatus" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="False">
            <ContentTemplate>
                <asp:Table ID="TableButtons" runat="server" CssClass="table" HorizontalAlign="Center" Width="0">
                    <asp:TableHeaderRow runat="server">
                        <asp:TableHeaderCell Scope="Column" ColumnSpan="8" Text="Functions" HorizontalAlign="Center" />
                    </asp:TableHeaderRow>
                    <asp:TableRow>
                        <asp:TableCell>
                            <asp:Button ID="ButtonStartHost" runat="server" CssClass="btn" Text="Start Host" OnClick="ButtonStartHost_Click" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonStopHost" runat="server" CssClass="btn" Text="Stop Host" OnClick="ButtonStopHost_Click" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonConnect" runat="server" CssClass="btn" Text="Connect" OnClick="ButtonConnect_OnClick" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonDisconnect" runat="server" CssClass="btn" Text="Disconnect" OnClick="ButtonDisconnect_OnClick" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonCreateOptions" runat="server" CssClass="btn" Text="Create Options" OnClick="ButtonCreateOptions_OnClick" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonModifyFa" runat="server" CssClass="btn" Text="Modify FA" OnClick="ButtonModifyFa_OnClick" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonExecuteTal" runat="server" CssClass="btn" Text="Execute TAL" OnClick="ButtonExecuteTal_OnClick" />
                        </asp:TableCell>
                        <asp:TableCell>
                            <asp:Button ID="ButtonAbort" runat="server" CssClass="btn" Text="Abort" OnClick="ButtonAbort_OnClick" />
                        </asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
                <asp:Table ID="TableOptions" runat="server" CssClass="table" HorizontalAlign="Center" Width="100%">
                    <asp:TableRow>
                        <asp:TableCell>
                            <asp:DropDownList ID="DropDownListOptionType" CssClass="dropdown" runat="server" OnSelectedIndexChanged="DropDownListOptionType_OnSelectedIndexChanged" AutoPostBack="True">
                            </asp:DropDownList>
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell>
                            <asp:CheckBoxList ID="CheckBoxListOptions" runat="server" CssClass="checkbox" CellPadding="5" CellSpacing="5" RepeatColumns="1" RepeatLayout="Table" RepeatDirection="Horizontal" TextAlign="Right" OnSelectedIndexChanged="CheckBoxListOptions_OnSelectedIndexChanged" AutoPostBack="True">
                            </asp:CheckBoxList>
                        </asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
                <asp:Table ID="TableStatus" runat="server" CssClass="table" HorizontalAlign="Center" Width="100%">
                    <asp:TableRow>
                        <asp:TableCell>
                            <asp:TextBox ID="TextBoxStatus" runat="server" CssClass="text-left" ReadOnly="True" TextMode="MultiLine" Rows="10"></asp:TextBox>
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell>
                            <asp:TextBox ID="TextBoxProgress" runat="server" CssClass="text-left" ReadOnly="True" TextMode="SingleLine"></asp:TextBox>
                        </asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
                <asp:Timer ID="TimerUpdate" runat="server" Interval="5000" OnTick="TimerUpdate_Tick">
                </asp:Timer>
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
</asp:Content>
