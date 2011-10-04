<%@ Page Title="Throw exception with data" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ExceptionData.aspx.cs" Inherits="ElmahModSampleSite.ExceptionData1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
   An exception should be in the <a href="~/Elmah.axd" runat="server">log</a>, go see for yourself!
</asp:Content>
