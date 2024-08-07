﻿using Aiursoft.UiStack;
using Aiursoft.UiStack.Layout;
using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.Navbar;
using Aiursoft.UiStack.Views.Shared.Components.SideAdvertisement;
using Aiursoft.UiStack.Views.Shared.Components.Sidebar;
using Aiursoft.UiStack.Views.Shared.Components.SideLogo;
using Aiursoft.UiStack.Views.Shared.Components.SideMenu;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var model = new UiStackLayoutViewModel
        {
            PageTitle = "Badge Generator",
            AppName = "ManHours",
            Theme = UiTheme.Dark,
            SidebarTheme = UiSidebarTheme.Dark,
            Layout = UiLayout.Fluid,
            Navbar = new NavbarViewModel
            {
                MegaMenu = new MegaMenuViewModel
                {
                    MenuName = "My Mega Menu",
                    DropDowns =
                    [
                        new DropDown
                        {
                            Header = "Home",
                            Links =
                            [
                                new Link { Text = "Index", Href = "/" },
                                new Link { Text = "Privacy", Href = "/" },
                                new Link { Text = "About", Href = "/" },
                                new Link { Text = "Badge", Href = "/" },
                            ]
                        },
                        new DropDown
                        {
                            Header = "Account",
                            Links =
                            [
                                new Link { Text = "Sign in", Href = "/" },
                                new Link { Text = "Sign up", Href = "/" },
                            ]
                        }
                    ]
                }
            },
            Sidebar = new SidebarViewModel
            {
                SideLogo = new SideLogoViewModel
                {
                    AppName = "Aiursoft UI Stack",
                    LogoUrl = "https://gitlab.aiursoft.cn/anduin/anduinos/-/raw/master/src/patches/logo/logo.svg",
                    Href = "#"
                },
                SideMenu = new SideMenuViewModel
                {
                    Groups =
                    [
                        new NavGroup
                        {
                            Name = "Navigation",
                            Items =
                            [
                                new CascadedSideBarItem
                                {
                                    UniqueId = "dashboards",
                                    Text = "Dashboards",
                                    IsActive = false,
                                    LucideIcon = "sliders",
                                    Decoration = new Decoration
                                    {
                                        Text = "5",
                                        ColorClass = "primary"
                                    },
                                    Links =
                                    [
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Default"
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Analytics"
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "SaaS"
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Social"
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Crypto"
                                        }
                                    ]
                                }
                            ]
                        },
                        new NavGroup
                        {
                            Name = "Apps",
                            Items =
                            [
                                new CascadedSideBarItem
                                {
                                    UniqueId = "ecommerce",
                                    Text = "E-Commerce",
                                    IsActive = true,
                                    LucideIcon = "shopping-bag",
                                    Links =
                                    [
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            IsActive = true,
                                            Text = "Products",
                                            Decoration = new Decoration
                                            {
                                                Text = "New",
                                                ColorClass = "primary"
                                            }
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Product Details",
                                            Decoration = new Decoration
                                            {
                                                Text = "New",
                                                ColorClass = "primary"
                                            }
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Orders",
                                            Decoration = new Decoration
                                            {
                                                Text = "New",
                                                ColorClass = "primary"
                                            }
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Customers",
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Invoice",
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Pricing",
                                        }
                                    ]
                                },
                                new CascadedSideBarItem
                                {
                                    UniqueId = "projects",
                                    Text = "Projects",
                                    LucideIcon = "layout",
                                    Links = 
                                    [
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Overview",
                                        },
                                        new CascadedLink
                                        {
                                            Href = "#",
                                            Text = "Details",
                                        },
                                    ]
                                },
                                new LinkSideBarItem
                                {
                                    Text = "Chat",
                                    LucideIcon = "list",
                                    Href = "#"
                                }
                            ]
                        }
                    ]
                },
                SideAdvertisement = new SideAdvertisementViewModel
                {
                    Title = "Download Native App",
                    Description = "Get the best experience with our app.",
                    Href = "#",
                    ButtonText = "Download"
                },
            },
            FooterMenu = new FooterMenuViewModel
            {
                AppBrand = new Link { Text = "ManHours", Href = "https://www.youtube.com/results?search_query=test" },
                Links =
                [
                    new Link { Text = "Home", Href = "/" },
                    new Link { Text = "Privacy", Href = "/" },
                    new Link { Text = "About", Href = "/" },
                    new Link { Text = "Badge", Href = "/" },
                ]
            }
        };
        return this.UiStackView(model);
    }
}