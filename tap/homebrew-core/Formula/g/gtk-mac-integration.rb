class GtkMacIntegration < Formula
  desc "GtkosxApplication API for GTK 3 on Windows (taskbar, menus, install paths)"
  homepage "https://www.gtk.org/docs/installations/macos"
  url "https://download.gnome.org/sources/gtk-mac-integration/3.0/gtk-mac-integration-3.0.2.tar.xz"
  sha256 "42f29e002365467eac10f4ba78435d4be785a947424d9890112c8c8d5e21be25"
  license "LGPL-2.1-only"

  # gtk-mac-integration is Objective-C on Cocoa: it moves a GTK menu bar into the macOS global menu
  # and drives the Dock and the app bundle. Nothing like it exists for Windows because GTK's Win32
  # backend already behaves natively, so applications that call the GtkosxApplication API would
  # simply fail to build. This formula provides that API for Windows as a header-only
  # implementation over GTK 3 and Win32 (taskbar icon and flashing, activation and termination
  # signals, documents from the command line, install-prefix paths, VERSIONINFO bundle info), with
  # upstream's include path and gtk-mac-integration-gtk3.pc, so `#ifdef MAC_INTEGRATION` code
  # compiles and works unchanged against GTK 3 (e.g. MSYS2's mingw-w64-ucrt-x86_64-gtk3).

  livecheck do
    url :stable
  end

  def install
    (include/"gtkmacintegration/gtkosxapplication.h").write <<~'C'
      /* gtkosxapplication.h for Windows (brew-windows' gtk-mac-integration formula)
       *
       * gtk-mac-integration moves a GTK application's menu bar into the macOS global menu and talks to
       * the Dock and the app bundle. This header implements the same GtkosxApplication API for GTK 3 on
       * Windows, on the Windows counterparts of those things, so applications written against
       * gtk-mac-integration build and behave natively on Windows:
       *
       *   menu bar                 stays in the window, which is where Windows menu bars live; items
       *                            for the macOS application menu (About, Preferences, Quit ...) are
       *                            appended to the menu bar's first menu, after a separator
       *   Dock icon                the taskbar icon (gtk_window_set_default_icon)
       *   Dock attention bounce    taskbar flashing (FlashWindowEx)
       *   app activation signals   NSApplicationDidBecomeActive / WillResignActive follow the menu bar
       *                            window's is-active state
       *   termination signals      closing the menu bar window emits NSApplicationBlockTermination
       *                            (a TRUE handler keeps it open) and then NSApplicationWillTerminate
       *   NSApplicationOpenFile    emitted by gtkosx_application_ready() for each file named on the
       *                            command line, which is how Explorer hands documents to a program
       *   bundle paths             the installation prefix (the directory above bin\), the executable
       *   bundle id / info         the process AppUserModelID, the executable's VERSIONINFO strings
       *
       * The Dock menu and the Window menu have no Windows counterpart; set_dock_menu and set_window_menu
       * keep the menus but do not show them elsewhere. Everything is static inline (no library to link
       * beyond GTK and a few system DLLs: see gtk-mac-integration-gtk3.pc). */

      #ifndef __GTK_OSX_APPLICATION_H__
      #define __GTK_OSX_APPLICATION_H__

      #include <gtk/gtk.h>
      #include <glib-object.h>
      #include <gdk/gdkwin32.h>
      #include <windows.h>
      #include <shellapi.h>
      #include <shobjidl.h>
      #include <string.h>

      G_BEGIN_DECLS

      typedef struct _GtkosxApplication GtkosxApplication;
      typedef struct _GtkosxApplicationClass GtkosxApplicationClass;
      typedef struct _GtkosxApplicationPrivate GtkosxApplicationPrivate;
      typedef struct _GtkosxApplicationMenuGroup GtkosxApplicationMenuGroup;

      struct _GtkosxApplication
      {
        GObject parent_instance;
        /*< private >*/
        GtkosxApplicationPrivate *priv;
      };

      struct _GtkosxApplicationClass
      {
        GObjectClass parent_class;
      };

      struct _GtkosxApplicationMenuGroup
      {
        GList *items;
      };

      typedef enum {
      #ifndef GTKOSX_DISABLE_DEPRECATED
        CRITICAL_REQUEST = 0,
        INFO_REQUEST = 10,
      #endif
        GTKOSX_APPLICATION_ATTENTION_TYPE_CRITICAL_REQUEST = 0,
        GTKOSX_APPLICATION_ATTENTION_TYPE_INFO_REQUEST = 10
      } GtkosxApplicationAttentionType;

      struct _GtkosxApplicationPrivate
      {
        GtkMenuShell *menu_bar;
        GtkWidget *window;            /* toplevel holding the menu bar */
        GtkMenuItem *window_menu;
        GtkMenuItem *help_menu;
        GtkMenuShell *dock_menu;
        GtkWidget *about_item;
        GList *app_items;             /* items for the application menu, in order */
        GtkWidget *app_separator;
        gboolean use_quartz_accelerators;
        gint next_attention_id;
        gint flashing;                /* id of the attention request in progress, 0 if none */
      };

      static inline GType gtkosx_application_get_type (void);

      #define GTKOSX_TYPE_APPLICATION (gtkosx_application_get_type ())
      #define GTKOSX_APPLICATION(obj) (G_TYPE_CHECK_INSTANCE_CAST ((obj), GTKOSX_TYPE_APPLICATION, GtkosxApplication))
      #define GTKOSX_IS_APPLICATION(obj) (G_TYPE_CHECK_INSTANCE_TYPE ((obj), GTKOSX_TYPE_APPLICATION))
      #define GTKOSX_APPLICATION_CLASS(klass) (G_TYPE_CHECK_CLASS_CAST ((klass), GTKOSX_TYPE_APPLICATION, GtkosxApplicationClass))
      #define GTKOSX_IS_APPLICATION_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), GTKOSX_TYPE_APPLICATION))
      #define GTKOSX_APPLICATION_GET_CLASS(obj) (G_TYPE_INSTANCE_GET_CLASS ((obj), GTKOSX_TYPE_APPLICATION, GtkosxApplicationClass))

      /* ---- the GObject type, shared by every translation unit that includes this header ---- */

      static inline GQuark
      gtkosx_application_singleton_quark_ (void)
      {
        return g_quark_from_static_string ("gtkosx-application-singleton");
      }

      static inline void
      gtkosx_application_finalize_ (GObject *object)
      {
        GtkosxApplication *self = (GtkosxApplication *) object;
        g_list_free (self->priv->app_items);
        g_free (self->priv);
        G_OBJECT_CLASS (g_type_class_peek_parent (G_OBJECT_GET_CLASS (object)))->finalize (object);
      }

      /* One application object per process, however it is created (gtkosx_application_get() or
       * g_object_new (GTKOSX_TYPE_APPLICATION, NULL) as older code does). */
      static inline GObject *
      gtkosx_application_constructor_ (GType type, guint n_properties, GObjectConstructParam *properties)
      {
        GObject *self = (GObject *) g_type_get_qdata (type, gtkosx_application_singleton_quark_ ());
        if (self != NULL)
          return g_object_ref (self);
        self = G_OBJECT_CLASS (g_type_class_peek_parent (g_type_class_peek (type)))->constructor (type, n_properties, properties);
        g_type_set_qdata (type, gtkosx_application_singleton_quark_ (), self);
        return self;
      }

      static inline void
      gtkosx_application_class_init_ (gpointer klass, gpointer data)
      {
        GObjectClass *object_class = G_OBJECT_CLASS (klass);
        GType type = G_TYPE_FROM_CLASS (klass);
        (void) data;
        object_class->constructor = gtkosx_application_constructor_;
        object_class->finalize = gtkosx_application_finalize_;
        g_signal_new ("NSApplicationDidBecomeActive", type, G_SIGNAL_NO_RECURSE | G_SIGNAL_ACTION,
                      0, NULL, NULL, NULL, G_TYPE_NONE, 0);
        g_signal_new ("NSApplicationWillResignActive", type, G_SIGNAL_NO_RECURSE | G_SIGNAL_ACTION,
                      0, NULL, NULL, NULL, G_TYPE_NONE, 0);
        g_signal_new ("NSApplicationBlockTermination", type, G_SIGNAL_NO_RECURSE | G_SIGNAL_ACTION,
                      0, g_signal_accumulator_true_handled, NULL, NULL, G_TYPE_BOOLEAN, 0);
        g_signal_new ("NSApplicationWillTerminate", type, G_SIGNAL_NO_RECURSE | G_SIGNAL_ACTION,
                      0, NULL, NULL, NULL, G_TYPE_NONE, 0);
        g_signal_new ("NSApplicationOpenFile", type, G_SIGNAL_NO_RECURSE | G_SIGNAL_ACTION,
                      0, NULL, NULL, NULL, G_TYPE_BOOLEAN, 1, G_TYPE_STRING);
      }

      static inline void
      gtkosx_application_instance_init_ (GTypeInstance *instance, gpointer klass)
      {
        GtkosxApplication *self = (GtkosxApplication *) instance;
        (void) klass;
        self->priv = g_new0 (GtkosxApplicationPrivate, 1);
        self->priv->next_attention_id = 1;
      }

      static inline GType
      gtkosx_application_get_type (void)
      {
        static GType type = 0;
        if (type == 0)
          {
            /* Another translation unit (or library) may have registered it already. */
            type = g_type_from_name ("GtkosxApplication");
            if (type == 0)
              type = g_type_register_static_simple (G_TYPE_OBJECT, g_intern_static_string ("GtkosxApplication"),
                                                    sizeof (GtkosxApplicationClass), gtkosx_application_class_init_,
                                                    sizeof (GtkosxApplication), gtkosx_application_instance_init_, 0);
          }
        return type;
      }

      static inline GtkosxApplication *
      gtkosx_application_get (void)
      {
        GtkosxApplication *self = (GtkosxApplication *) g_type_get_qdata (GTKOSX_TYPE_APPLICATION,
                                                                          gtkosx_application_singleton_quark_ ());
        if (self == NULL)
          self = (GtkosxApplication *) g_object_new (GTKOSX_TYPE_APPLICATION, NULL);
        return self;
      }

      /* ---- helpers ---- */

      static inline gchar *
      gtkosx_application_utf8_ (const wchar_t *text)
      {
        return text != NULL ? g_utf16_to_utf8 ((const gunichar2 *) text, -1, NULL, NULL, NULL) : NULL;
      }

      static inline HWND
      gtkosx_application_hwnd_ (GtkosxApplication *self)
      {
        GtkWidget *window = self->priv->window;
        if (window == NULL)
          {
            /* No menu bar window: the first visible toplevel stands in for the application. */
            GList *toplevels = gtk_window_list_toplevels (), *l;
            for (l = toplevels; l != NULL && window == NULL; l = l->next)
              if (gtk_widget_get_visible (GTK_WIDGET (l->data)) && gtk_widget_get_realized (GTK_WIDGET (l->data)))
                window = GTK_WIDGET (l->data);
            g_list_free (toplevels);
          }
        if (window == NULL || !gtk_widget_get_realized (window))
          return NULL;
        return (HWND) gdk_win32_window_get_handle (gtk_widget_get_window (window));
      }

      static inline void
      gtkosx_application_notify_active_ (GObject *window, GParamSpec *pspec, gpointer data)
      {
        (void) pspec;
        g_signal_emit_by_name (data, gtk_window_is_active (GTK_WINDOW (window))
                                       ? "NSApplicationDidBecomeActive" : "NSApplicationWillResignActive");
      }

      static inline gboolean
      gtkosx_application_delete_event_ (GtkWidget *window, GdkEvent *event, gpointer data)
      {
        gboolean blocked = FALSE;
        (void) window; (void) event;
        g_signal_emit_by_name (data, "NSApplicationBlockTermination", &blocked);
        if (blocked)
          return TRUE;
        g_signal_emit_by_name (data, "NSApplicationWillTerminate");
        return FALSE;
      }

      static inline void
      gtkosx_application_bind_window_ (GtkosxApplication *self)
      {
        GtkWidget *toplevel;
        if (self->priv->menu_bar == NULL)
          return;
        toplevel = gtk_widget_get_toplevel (GTK_WIDGET (self->priv->menu_bar));
        if (!GTK_IS_WINDOW (toplevel) || toplevel == self->priv->window)
          return;
        if (self->priv->window != NULL)
          {
            g_signal_handlers_disconnect_by_data (self->priv->window, self);
            g_object_remove_weak_pointer (G_OBJECT (self->priv->window), (gpointer *) &self->priv->window);
          }
        self->priv->window = toplevel;
        g_object_add_weak_pointer (G_OBJECT (toplevel), (gpointer *) &self->priv->window);
        g_signal_connect (toplevel, "notify::is-active", G_CALLBACK (gtkosx_application_notify_active_), self);
        g_signal_connect (toplevel, "delete-event", G_CALLBACK (gtkosx_application_delete_event_), self);
      }

      static inline void
      gtkosx_application_hierarchy_changed_ (GtkWidget *widget, GtkWidget *previous, gpointer data)
      {
        (void) widget; (void) previous;
        gtkosx_application_bind_window_ ((GtkosxApplication *) data);
      }

      /* The first submenu of the menu bar ("File" in Windows programs), where application-menu items go. */
      static inline GtkMenuShell *
      gtkosx_application_first_menu_ (GtkosxApplication *self)
      {
        GList *children, *l;
        GtkMenuShell *menu = NULL;
        if (self->priv->menu_bar == NULL)
          return NULL;
        children = gtk_container_get_children (GTK_CONTAINER (self->priv->menu_bar));
        for (l = children; l != NULL && menu == NULL; l = l->next)
          if (GTK_IS_MENU_ITEM (l->data) && GTK_IS_MENU_SHELL (gtk_menu_item_get_submenu (GTK_MENU_ITEM (l->data))))
            menu = GTK_MENU_SHELL (gtk_menu_item_get_submenu (GTK_MENU_ITEM (l->data)));
        g_list_free (children);
        return menu;
      }

      static inline void
      gtkosx_application_place_app_items_ (GtkosxApplication *self)
      {
        GtkMenuShell *menu = gtkosx_application_first_menu_ (self);
        GList *l;
        if (menu == NULL || self->priv->app_items == NULL)
          return;
        if (self->priv->app_separator == NULL)
          {
            self->priv->app_separator = gtk_separator_menu_item_new ();
            gtk_menu_shell_append (menu, self->priv->app_separator);
            gtk_widget_show (self->priv->app_separator);
          }
        for (l = self->priv->app_items; l != NULL; l = l->next)
          {
            GtkWidget *item = GTK_WIDGET (l->data);
            GtkWidget *parent = gtk_widget_get_parent (item);
            if (parent == GTK_WIDGET (menu))
              gtk_container_remove (GTK_CONTAINER (menu), item);
            else if (parent != NULL)
              continue;               /* the application packed it somewhere itself */
            gtk_menu_shell_append (menu, item);
            gtk_widget_show (item);
          }
      }

      /* ---- the GtkosxApplication API ---- */

      /* Opens the documents named on the command line, the way macOS delivers files opened from Finder. */
      static inline void
      gtkosx_application_ready (GtkosxApplication *self)
      {
        int argc = 0, i;
        wchar_t **argv = CommandLineToArgvW (GetCommandLineW (), &argc);
        if (argv == NULL)
          return;
        for (i = 1; i < argc; i++)
          {
            gchar *path = gtkosx_application_utf8_ (argv[i]);
            gboolean handled = FALSE;
            if (path != NULL && path[0] != '-' && g_file_test (path, G_FILE_TEST_EXISTS))
              g_signal_emit_by_name (self, "NSApplicationOpenFile", path, &handled);
            g_free (path);
          }
        LocalFree (argv);
      }

      static inline void
      gtkosx_application_set_use_quartz_accelerators (GtkosxApplication *self, gboolean use_quartz_accelerators)
      {
        self->priv->use_quartz_accelerators = use_quartz_accelerators;
      }

      /* Windows has no Command key: accelerators stay on Ctrl whatever this is set to. */
      static inline gboolean
      gtkosx_application_use_quartz_accelerators (GtkosxApplication *self)
      {
        return self->priv->use_quartz_accelerators;
      }

      static inline void
      gtkosx_application_set_menu_bar (GtkosxApplication *self, GtkMenuShell *menu_shell)
      {
        if (self->priv->menu_bar != NULL)
          g_signal_handlers_disconnect_by_data (self->priv->menu_bar, self);
        self->priv->menu_bar = menu_shell;
        if (menu_shell == NULL)
          return;
        g_signal_connect (menu_shell, "hierarchy-changed", G_CALLBACK (gtkosx_application_hierarchy_changed_), self);
        gtkosx_application_bind_window_ (self);
        gtkosx_application_place_app_items_ (self);
      }

      /* The menu bar in the window is the live one, so there is nothing to copy. */
      static inline void
      gtkosx_application_sync_menubar (GtkosxApplication *self)
      {
        gtkosx_application_bind_window_ (self);
        gtkosx_application_place_app_items_ (self);
      }

      static inline void
      gtkosx_application_insert_app_menu_item (GtkosxApplication *self, GtkWidget *menu_item, gint index)
      {
        if (menu_item == NULL || g_list_find (self->priv->app_items, menu_item) != NULL)
          return;
        g_object_ref_sink (menu_item);
        self->priv->app_items = g_list_insert (self->priv->app_items, menu_item, index);
        gtkosx_application_place_app_items_ (self);
      }

      static inline void
      gtkosx_application_set_about_item (GtkosxApplication *self, GtkWidget *item)
      {
        self->priv->about_item = item;
        gtkosx_application_insert_app_menu_item (self, item, 0);
      }

      static inline void
      gtkosx_application_set_window_menu (GtkosxApplication *self, GtkMenuItem *menu_item)
      {
        self->priv->window_menu = menu_item;
      }

      static inline void
      gtkosx_application_set_help_menu (GtkosxApplication *self, GtkMenuItem *menu_item)
      {
        self->priv->help_menu = menu_item;
      }

      static inline void
      gtkosx_application_set_dock_menu (GtkosxApplication *self, GtkMenuShell *menu_shell)
      {
        self->priv->dock_menu = menu_shell;
      }

      /* The Dock icon's counterpart is the taskbar icon of every window of the application. */
      static inline void
      gtkosx_application_set_dock_icon_pixbuf (GtkosxApplication *self, GdkPixbuf *pixbuf)
      {
        GList *toplevels, *l;
        (void) self;
        if (pixbuf == NULL)
          return;
        gtk_window_set_default_icon (pixbuf);
        toplevels = gtk_window_list_toplevels ();
        for (l = toplevels; l != NULL; l = l->next)
          gtk_window_set_icon (GTK_WINDOW (l->data), pixbuf);
        g_list_free (toplevels);
      }

      static inline gchar *gtkosx_application_get_resource_path (void);

      static inline void
      gtkosx_application_set_dock_icon_resource (GtkosxApplication *self, const gchar *name,
                                                 const gchar *type, const gchar *subdir)
      {
        gchar *resources = gtkosx_application_get_resource_path ();
        gchar *file = type != NULL && *type ? g_strconcat (name, ".", type, NULL) : g_strdup (name);
        gchar *path = g_build_filename (resources, subdir != NULL ? subdir : "", file, NULL);
        GdkPixbuf *pixbuf = gdk_pixbuf_new_from_file (path, NULL);
        if (pixbuf != NULL)
          {
            gtkosx_application_set_dock_icon_pixbuf (self, pixbuf);
            g_object_unref (pixbuf);
          }
        g_free (path);
        g_free (file);
        g_free (resources);
      }

      /* Flashes the taskbar button: until the window comes to the front for a critical request, a few
       * times for an informational one. Returns an id for gtkosx_application_cancel_attention_request. */
      static inline gint
      gtkosx_application_attention_request (GtkosxApplication *self, GtkosxApplicationAttentionType type)
      {
        FLASHWINFO info;
        HWND hwnd = gtkosx_application_hwnd_ (self);
        if (hwnd == NULL)
          return 0;
        memset (&info, 0, sizeof info);
        info.cbSize = sizeof info;
        info.hwnd = hwnd;
        if (type == GTKOSX_APPLICATION_ATTENTION_TYPE_CRITICAL_REQUEST)
          info.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
        else
          {
            info.dwFlags = FLASHW_TRAY;
            info.uCount = 3;
          }
        FlashWindowEx (&info);
        self->priv->flashing = self->priv->next_attention_id++;
        return self->priv->flashing;
      }

      static inline void
      gtkosx_application_cancel_attention_request (GtkosxApplication *self, gint id)
      {
        FLASHWINFO info;
        HWND hwnd = gtkosx_application_hwnd_ (self);
        if (hwnd == NULL || id == 0 || id != self->priv->flashing)
          return;
        memset (&info, 0, sizeof info);
        info.cbSize = sizeof info;
        info.hwnd = hwnd;
        info.dwFlags = FLASHW_STOP;
        FlashWindowEx (&info);
        self->priv->flashing = 0;
      }

      /* ---- bundle functions: the installation prefix stands in for the .app bundle ---- */

      static inline gchar *
      gtkosx_application_get_executable_path (void)
      {
        wchar_t path[32768];
        DWORD length = GetModuleFileNameW (NULL, path, G_N_ELEMENTS (path));
        if (length == 0 || length >= G_N_ELEMENTS (path))
          return NULL;
        return gtkosx_application_utf8_ (path);
      }

      /* The directory above the executable's bin\ (or the executable's directory), where share\ lives. */
      static inline gchar *
      gtkosx_application_get_bundle_path (void)
      {
        return g_win32_get_package_installation_directory_of_module (NULL);
      }

      static inline gchar *
      gtkosx_application_get_resource_path (void)
      {
        return g_win32_get_package_installation_directory_of_module (NULL);
      }

      /* The process's explicit AppUserModelID (what Windows groups taskbar buttons by), or NULL. */
      static inline gchar *
      gtkosx_application_get_bundle_id (void)
      {
        PWSTR id = NULL;
        gchar *result = NULL;
        if (SUCCEEDED (GetCurrentProcessExplicitAppUserModelID (&id)) && id != NULL)
          {
            result = gtkosx_application_utf8_ (id);
            CoTaskMemFree (id);
          }
        return result;
      }

      /* Info.plist keys answered from the executable's VERSIONINFO resource. */
      static inline gchar *
      gtkosx_application_get_bundle_info (const gchar *key)
      {
        static const char *const map[][2] = {
          { "CFBundleName", "ProductName" },
          { "CFBundleDisplayName", "FileDescription" },
          { "CFBundleShortVersionString", "ProductVersion" },
          { "CFBundleVersion", "FileVersion" },
          { "CFBundleExecutable", "OriginalFilename" },
          { "CFBundleIdentifier", "InternalName" },
          { "CFBundleGetInfoString", "Comments" },
          { "NSHumanReadableCopyright", "LegalCopyright" },
        };
        const char *name = key;
        wchar_t module[32768], query[256];
        DWORD handle = 0, size;
        void *data;
        WORD *translation;
        UINT length = 0;
        wchar_t *value = NULL;
        gchar *result = NULL;
        gsize i;
        if (key == NULL)
          return NULL;
        if (strcmp (key, "CFBundleIdentifier") == 0 && (result = gtkosx_application_get_bundle_id ()) != NULL)
          return result;
        for (i = 0; i < G_N_ELEMENTS (map); i++)
          if (strcmp (key, map[i][0]) == 0)
            name = map[i][1];
        if (GetModuleFileNameW (NULL, module, G_N_ELEMENTS (module)) == 0)
          return NULL;
        size = GetFileVersionInfoSizeW (module, &handle);
        if (size == 0)
          return NULL;
        data = g_malloc (size);
        if (GetFileVersionInfoW (module, 0, size, data)
            && VerQueryValueW (data, L"\\VarFileInfo\\Translation", (void **) &translation, &length)
            && length >= 2 * sizeof (WORD))
          {
            wchar_t *wname = (wchar_t *) g_utf8_to_utf16 (name, -1, NULL, NULL, NULL);
            _snwprintf (query, G_N_ELEMENTS (query), L"\\StringFileInfo\\%04x%04x\\%ls",
                        translation[0], translation[1], wname);
            query[G_N_ELEMENTS (query) - 1] = 0;
            if (VerQueryValueW (data, query, (void **) &value, &length) && length > 0)
              result = gtkosx_application_utf8_ (value);
            g_free (wname);
          }
        g_free (data);
        return result;
      }

      G_END_DECLS

      #endif /* __GTK_OSX_APPLICATION_H__ */
    C
    (lib/"pkgconfig/gtk-mac-integration-gtk3.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: gtk-mac-integration-gtk3
      Description: GtkosxApplication API for GTK 3 on Windows
      Requires: gtk+-3.0
      Version: #{version}
      Libs: -L${libdir} -lgtkmacintegration-gtk3 -lversion -lshell32 -lole32
      Cflags: -I${includedir}/gtkmacintegration -DMAC_INTEGRATION
    PC
    # The implementation is all inline; an empty archive keeps upstream's -lgtkmacintegration-gtk3 working.
    (lib/"libgtkmacintegration-gtk3.a").binwrite "!<arch>\n"
    doc.install "README", "COPYING"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the API the header provides and the pkg-config file.
    header = (include/"gtkmacintegration/gtkosxapplication.h").read
    %w[gtkosx_application_get gtkosx_application_set_menu_bar gtkosx_application_attention_request
       gtkosx_application_get_bundle_path NSApplicationBlockTermination FlashWindowEx].each do |symbol|
      assert_match symbol, header
    end
    pc = (lib/"pkgconfig/gtk-mac-integration-gtk3.pc").read
    assert_match "-I${includedir}/gtkmacintegration -DMAC_INTEGRATION", pc
    assert_match "Requires: gtk+-3.0", pc
    assert_equal "!<arch>\n", (lib/"libgtkmacintegration-gtk3.a").binread
  end
end
