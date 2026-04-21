<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EmailVerification.aspx.cs" Inherits="SelfServiceAPI.EmailVerification" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style>
        .center {
            margin: 0 auto;
            border: 5px;
            padding: 20px;
        }
    </style>
</head>

   
<body>
    <form id="form1" runat="server">
        <div class="center">
            <img src="Images/logo.png"  />
           
        </div>

        <div class="center">
             <asp:Label ID="lblMsg" runat="server" visible="false" Font-Size="X-Large"  ForeColor="Black"></asp:Label>

            <p runat="server" id="redirection" visible="false" style="font-size:x-large;color:black">Click 
            <a id="appLink"  style="font-size:x-large;color:black" runat="server">here</a>   
              to access your application.</p>
    </div>
    </form>
</body>
</html>
