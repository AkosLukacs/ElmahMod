<%@ Page Title="Throw exception with an InnerException" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="InnerException.aspx.cs" Inherits="ElmahModSampleSite.InnerException1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
   An exception should be in the <a href="~/Elmah.axd" runat="server">log</a>, go see for yourself!
</asp:Content>
