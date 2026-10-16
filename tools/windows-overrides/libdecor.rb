class Libdecor < Formula
  desc "Client-side decorations library for Wayland client"
  homepage "https://gitlab.freedesktop.org/libdecor/libdecor"
  url "https://gitlab.freedesktop.org/libdecor/libdecor/-/releases/0.2.5/downloads/libdecor-0.2.5.tar.xz"
  sha256 "7fd50f780a4fee90a03f7b2c09055033e488654cbaff4a0c4bbae616bac9cd1c"
  license "MIT"

  # Windows has no Wayland: window decorations are drawn by the desktop window manager (DWM). This
  # keg is libdecor's public API (include/libdecor-0/libdecor.h, libdecor-0.dll, libdecor-0.pc)
  # implemented on Win32: a frame is a native, DWM-decorated top-level window whose title,
  # capabilities, min/max content size, maximize, minimize and fullscreen map to the window's styles
  # and placement; WM_SIZE, activation, snapping and state changes arrive as configure events,
  # WM_CLOSE as close, and libdecor_dispatch() pumps the thread's window messages. The application's
  # content window (an HWND, passed where upstream takes a wl_surface) is hosted in the frame.

  livecheck do
    url "https://gitlab.freedesktop.org/api/v4/projects/18349/releases"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :json do |json, regex|
      json.filter_map { |item| item["tag_name"]&.[](regex, 1) }
    end
  end

  def install
    (buildpath/"win/libdecor.h").write <<~'C'
      /*
       * libdecor - client-side window decorations, Windows edition.
       *
       * The same API as upstream libdecor 0.2. On Windows the "compositor" is the desktop window
       * manager: a frame is a native top-level window that DWM decorates (title bar, borders, the
       * minimize/maximize/close buttons, snap layouts). Where upstream takes Wayland objects:
       *   - libdecor_new() ignores the wl_display (pass NULL);
       *   - libdecor_decorate() takes the application's content window as the "surface": an HWND cast
       *     to struct wl_surface *, which becomes a child filling the frame's client area, or NULL to
       *     draw into the frame's own client area (libdecor_frame_get_window() returns its HWND);
       *   - wl_seat and wl_output arguments are ignored (there is one pointer and the frame's monitor);
       *   - libdecor_get_fd() returns -1: libdecor_dispatch() pumps the thread's window messages.
       */
      #ifndef LIBDECOR_H
      #define LIBDECOR_H

      #include <stdbool.h>
      #include <stdint.h>

      #if defined(__has_include)
      #if __has_include(<wayland-client.h>)
      #include <wayland-client.h>
      #define LIBDECOR_HAVE_WAYLAND_CLIENT 1
      #endif
      #endif

      #ifdef __cplusplus
      extern "C" {
      #endif

      #ifndef LIBDECOR_HAVE_WAYLAND_CLIENT
      struct wl_display;
      struct wl_surface;
      struct wl_seat;
      struct wl_output;
      #endif

      #if defined(_WIN32) && defined(LIBDECOR_BUILDING)
      #define LIBDECOR_EXPORT __declspec(dllexport)
      #else
      #define LIBDECOR_EXPORT
      #endif

      struct xdg_surface;
      struct xdg_toplevel;
      struct libdecor;
      struct libdecor_frame;
      struct libdecor_configuration;
      struct libdecor_state;

      enum libdecor_error {
          LIBDECOR_ERROR_COMPOSITOR_INCOMPATIBLE,
          LIBDECOR_ERROR_INVALID_FRAME_CONFIGURATION,
      };

      enum libdecor_window_state {
          LIBDECOR_WINDOW_STATE_NONE = 0,
          LIBDECOR_WINDOW_STATE_ACTIVE = 1 << 0,
          LIBDECOR_WINDOW_STATE_MAXIMIZED = 1 << 1,
          LIBDECOR_WINDOW_STATE_FULLSCREEN = 1 << 2,
          LIBDECOR_WINDOW_STATE_TILED_LEFT = 1 << 3,
          LIBDECOR_WINDOW_STATE_TILED_RIGHT = 1 << 4,
          LIBDECOR_WINDOW_STATE_TILED_TOP = 1 << 5,
          LIBDECOR_WINDOW_STATE_TILED_BOTTOM = 1 << 6,
          LIBDECOR_WINDOW_STATE_SUSPENDED = 1 << 7,
      };

      enum libdecor_resize_edge {
          LIBDECOR_RESIZE_EDGE_NONE,
          LIBDECOR_RESIZE_EDGE_TOP,
          LIBDECOR_RESIZE_EDGE_BOTTOM,
          LIBDECOR_RESIZE_EDGE_LEFT,
          LIBDECOR_RESIZE_EDGE_TOP_LEFT,
          LIBDECOR_RESIZE_EDGE_BOTTOM_LEFT,
          LIBDECOR_RESIZE_EDGE_RIGHT,
          LIBDECOR_RESIZE_EDGE_TOP_RIGHT,
          LIBDECOR_RESIZE_EDGE_BOTTOM_RIGHT,
      };

      enum libdecor_capabilities {
          LIBDECOR_ACTION_MOVE = 1 << 0,
          LIBDECOR_ACTION_RESIZE = 1 << 1,
          LIBDECOR_ACTION_MINIMIZE = 1 << 2,
          LIBDECOR_ACTION_FULLSCREEN = 1 << 3,
          LIBDECOR_ACTION_CLOSE = 1 << 4,
      };

      struct libdecor_interface {
          /* An error event; the context is unusable afterwards. */
          void (* error)(struct libdecor *context, enum libdecor_error error, const char *message);

          /* Reserved */
          void (* reserved0)(void);
          void (* reserved1)(void);
          void (* reserved2)(void);
          void (* reserved3)(void);
          void (* reserved4)(void);
          void (* reserved5)(void);
          void (* reserved6)(void);
          void (* reserved7)(void);
          void (* reserved8)(void);
          void (* reserved9)(void);
      };

      struct libdecor_frame_interface {
          /* A new configuration (size, window state) was received; reply with libdecor_frame_commit(). */
          void (* configure)(struct libdecor_frame *frame, struct libdecor_configuration *configuration, void *user_data);
          /* The user asked to close the window. */
          void (* close)(struct libdecor_frame *frame, void *user_data);
          /* The decorations changed; commit (redraw) the main surface. */
          void (* commit)(struct libdecor_frame *frame, void *user_data);
          /* Any open popup of this frame should be dismissed. */
          void (* dismiss_popup)(struct libdecor_frame *frame, const char *seat_name, void *user_data);

          /* Reserved */
          void (* reserved0)(void);
          void (* reserved1)(void);
          void (* reserved2)(void);
          void (* reserved3)(void);
          void (* reserved4)(void);
          void (* reserved5)(void);
          void (* reserved6)(void);
          void (* reserved7)(void);
          void (* reserved8)(void);
          void (* reserved9)(void);
      };

      LIBDECOR_EXPORT void libdecor_unref(struct libdecor *context);
      LIBDECOR_EXPORT struct libdecor *libdecor_new(struct wl_display *display, struct libdecor_interface *iface);
      LIBDECOR_EXPORT int libdecor_get_fd(struct libdecor *context);
      LIBDECOR_EXPORT int libdecor_dispatch(struct libdecor *context, int timeout);
      LIBDECOR_EXPORT struct libdecor_frame *libdecor_decorate(struct libdecor *context, struct wl_surface *surface,
                                   struct libdecor_frame_interface *iface, void *user_data);
      LIBDECOR_EXPORT void libdecor_frame_ref(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_unref(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_set_visibility(struct libdecor_frame *frame, bool visible);
      LIBDECOR_EXPORT bool libdecor_frame_is_visible(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_set_parent(struct libdecor_frame *frame, struct libdecor_frame *parent);
      LIBDECOR_EXPORT void libdecor_frame_set_title(struct libdecor_frame *frame, const char *title);
      LIBDECOR_EXPORT const char *libdecor_frame_get_title(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_set_app_id(struct libdecor_frame *frame, const char *app_id);
      LIBDECOR_EXPORT void libdecor_frame_set_capabilities(struct libdecor_frame *frame, enum libdecor_capabilities capabilities);
      LIBDECOR_EXPORT void libdecor_frame_unset_capabilities(struct libdecor_frame *frame, enum libdecor_capabilities capabilities);
      LIBDECOR_EXPORT bool libdecor_frame_has_capability(struct libdecor_frame *frame, enum libdecor_capabilities capability);
      LIBDECOR_EXPORT void libdecor_frame_show_window_menu(struct libdecor_frame *frame, struct wl_seat *wl_seat,
                                   uint32_t serial, int x, int y);
      LIBDECOR_EXPORT void libdecor_frame_popup_grab(struct libdecor_frame *frame, const char *seat_name);
      LIBDECOR_EXPORT void libdecor_frame_popup_ungrab(struct libdecor_frame *frame, const char *seat_name);
      LIBDECOR_EXPORT void libdecor_frame_translate_coordinate(struct libdecor_frame *frame, int surface_x, int surface_y,
                                   int *frame_x, int *frame_y);
      LIBDECOR_EXPORT void libdecor_frame_set_min_content_size(struct libdecor_frame *frame, int content_width, int content_height);
      LIBDECOR_EXPORT void libdecor_frame_set_max_content_size(struct libdecor_frame *frame, int content_width, int content_height);
      LIBDECOR_EXPORT void libdecor_frame_get_min_content_size(const struct libdecor_frame *frame, int *content_width, int *content_height);
      LIBDECOR_EXPORT void libdecor_frame_get_max_content_size(const struct libdecor_frame *frame, int *content_width, int *content_height);
      LIBDECOR_EXPORT void libdecor_frame_resize(struct libdecor_frame *frame, struct wl_seat *wl_seat, uint32_t serial,
                             enum libdecor_resize_edge edge);
      LIBDECOR_EXPORT void libdecor_frame_move(struct libdecor_frame *frame, struct wl_seat *wl_seat, uint32_t serial);
      LIBDECOR_EXPORT void libdecor_frame_commit(struct libdecor_frame *frame, struct libdecor_state *state,
                             struct libdecor_configuration *configuration);
      LIBDECOR_EXPORT void libdecor_frame_set_minimized(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_set_maximized(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_unset_maximized(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_set_fullscreen(struct libdecor_frame *frame, struct wl_output *output);
      LIBDECOR_EXPORT void libdecor_frame_unset_fullscreen(struct libdecor_frame *frame);
      LIBDECOR_EXPORT bool libdecor_frame_is_floating(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_close(struct libdecor_frame *frame);
      LIBDECOR_EXPORT void libdecor_frame_map(struct libdecor_frame *frame);
      LIBDECOR_EXPORT struct xdg_surface *libdecor_frame_get_xdg_surface(struct libdecor_frame *frame);
      LIBDECOR_EXPORT struct xdg_toplevel *libdecor_frame_get_xdg_toplevel(struct libdecor_frame *frame);
      LIBDECOR_EXPORT struct libdecor_state *libdecor_state_new(int width, int height);
      LIBDECOR_EXPORT void libdecor_state_free(struct libdecor_state *state);
      LIBDECOR_EXPORT bool libdecor_configuration_get_content_size(struct libdecor_configuration *configuration,
                                       struct libdecor_frame *frame, int *width, int *height);
      LIBDECOR_EXPORT bool libdecor_configuration_get_window_state(struct libdecor_configuration *configuration,
                                       enum libdecor_window_state *window_state);

      #ifdef _WIN32
      /* Windows only: the frame's top-level window (an HWND). */
      LIBDECOR_EXPORT void *libdecor_frame_get_window(struct libdecor_frame *frame);
      #endif

      #ifdef __cplusplus
      }
      #endif

      #endif /* LIBDECOR_H */
    C

    (buildpath/"win/libdecor.c").write <<~'C'
      /* libdecor for Windows: each frame is a native top-level window decorated by DWM. Window
       * messages become libdecor events: WM_SIZE / WM_ACTIVATE / maximize, fullscreen and snap changes
       * are "configure" events carrying the content (client area) size and window state, WM_CLOSE is
       * "close", WM_GETMINMAXINFO enforces the content size limits. libdecor_dispatch() pumps the
       * calling thread's messages, the way upstream's dispatch reads the Wayland socket. */
      #define WIN32_LEAN_AND_MEAN
      #define LIBDECOR_BUILDING
      #include <windows.h>
      #include <shellapi.h>
      #include <stdlib.h>
      #include <string.h>
      #include "libdecor.h"

      #define WM_LIBDECOR_CONFIGURE (WM_APP + 0x1dec)
      #define FRAME_CLASS L"libdecor-frame"

      struct libdecor {
          int ref;
          struct libdecor_interface *iface;
          struct libdecor_frame *frames;
          bool failed;
      };

      struct libdecor_state {
          int width, height;
      };

      struct libdecor_configuration {
          bool has_size;
          int width, height;
          enum libdecor_window_state window_state;
      };

      struct libdecor_frame {
          int ref;
          struct libdecor *context;
          struct libdecor_frame *next;
          struct libdecor_frame_interface *iface;
          void *user_data;
          HWND hwnd, content;
          char *title, *app_id;
          enum libdecor_capabilities capabilities;
          int min_width, min_height, max_width, max_height;
          bool visible, mapped, fullscreen, configure_pending, in_commit;
          WINDOWPLACEMENT restore;
          LONG_PTR restore_style;
          enum libdecor_window_state last_state;
          int last_width, last_height;
      };

      static wchar_t *widen(const char *s)
      {
          int n;
          wchar_t *w;
          if (s == NULL) return NULL;
          n = MultiByteToWideChar(CP_UTF8, 0, s, -1, NULL, 0);
          w = malloc(sizeof(wchar_t) * (size_t)(n > 0 ? n : 1));
          if (w == NULL) return NULL;
          if (n <= 0) w[0] = 0;
          else MultiByteToWideChar(CP_UTF8, 0, s, -1, w, n);
          return w;
      }

      static char *dup_string(const char *s)
      {
          size_t n;
          char *d;
          if (s == NULL) return NULL;
          n = strlen(s) + 1;
          d = malloc(n);
          if (d != NULL) memcpy(d, s, n);
          return d;
      }

      static DWORD frame_style(const struct libdecor_frame *frame)
      {
          DWORD style = WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
          if (frame->fullscreen) return style | WS_POPUP;
          if (!frame->visible) return style | WS_POPUP | WS_THICKFRAME;
          style |= WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU;
          if (frame->capabilities & LIBDECOR_ACTION_RESIZE) style |= WS_THICKFRAME | WS_MAXIMIZEBOX;
          if (frame->capabilities & LIBDECOR_ACTION_MINIMIZE) style |= WS_MINIMIZEBOX;
          return style;
      }

      /* Window rectangle size for a client (content) size under the frame's current styles. */
      static void outer_size(const struct libdecor_frame *frame, int width, int height, int *outer_width, int *outer_height)
      {
          RECT r = { 0, 0, width, height };
          AdjustWindowRectEx(&r, (DWORD)GetWindowLongPtrW(frame->hwnd, GWL_STYLE), FALSE,
                             (DWORD)GetWindowLongPtrW(frame->hwnd, GWL_EXSTYLE));
          *outer_width = r.right - r.left;
          *outer_height = r.bottom - r.top;
      }

      static void apply_style(struct libdecor_frame *frame)
      {
          HMENU menu;
          LONG_PTR style = GetWindowLongPtrW(frame->hwnd, GWL_STYLE);
          LONG_PTR keep = style & (WS_VISIBLE | WS_MINIMIZE | WS_MAXIMIZE | WS_DISABLED);
          SetWindowLongPtrW(frame->hwnd, GWL_STYLE, (LONG_PTR)frame_style(frame) | keep);
          menu = GetSystemMenu(frame->hwnd, FALSE);
          if (menu != NULL)
              EnableMenuItem(menu, SC_CLOSE, MF_BYCOMMAND | ((frame->capabilities & LIBDECOR_ACTION_CLOSE) ? MF_ENABLED : MF_GRAYED));
          SetWindowPos(frame->hwnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
      }

      static enum libdecor_window_state current_state(struct libdecor_frame *frame)
      {
          enum libdecor_window_state state = LIBDECOR_WINDOW_STATE_NONE;
          if (GetForegroundWindow() == frame->hwnd) state |= LIBDECOR_WINDOW_STATE_ACTIVE;
          if (IsIconic(frame->hwnd) || !IsWindowVisible(frame->hwnd)) state |= LIBDECOR_WINDOW_STATE_SUSPENDED;
          if (frame->fullscreen) return state | LIBDECOR_WINDOW_STATE_FULLSCREEN;
          if (IsZoomed(frame->hwnd)) return state | LIBDECOR_WINDOW_STATE_MAXIMIZED;
          /* Aero Snap: a restored window that spans the work area's height (or width) at one edge. */
          {
              RECT window, work;
              MONITORINFO info = { sizeof(info) };
              if (GetWindowRect(frame->hwnd, &window) && GetMonitorInfoW(MonitorFromWindow(frame->hwnd, MONITOR_DEFAULTTONEAREST), &info)) {
                  int slack = GetSystemMetrics(SM_CXSIZEFRAME) * 2 + 8;
                  work = info.rcWork;
                  bool full_height = abs(window.top - work.top) <= slack && abs(window.bottom - work.bottom) <= slack;
                  bool full_width = abs(window.left - work.left) <= slack && abs(window.right - work.right) <= slack;
                  if (full_height && !full_width) {
                      if (abs(window.left - work.left) <= slack) state |= LIBDECOR_WINDOW_STATE_TILED_LEFT;
                      if (abs(window.right - work.right) <= slack) state |= LIBDECOR_WINDOW_STATE_TILED_RIGHT;
                      state |= LIBDECOR_WINDOW_STATE_TILED_TOP | LIBDECOR_WINDOW_STATE_TILED_BOTTOM;
                  }
                  else if (full_width && !full_height) {
                      if (abs(window.top - work.top) <= slack) state |= LIBDECOR_WINDOW_STATE_TILED_TOP;
                      if (abs(window.bottom - work.bottom) <= slack) state |= LIBDECOR_WINDOW_STATE_TILED_BOTTOM;
                      state |= LIBDECOR_WINDOW_STATE_TILED_LEFT | LIBDECOR_WINDOW_STATE_TILED_RIGHT;
                  }
              }
          }
          return state;
      }

      static void layout_content(struct libdecor_frame *frame)
      {
          RECT client;
          if (frame->content == NULL || !GetClientRect(frame->hwnd, &client)) return;
          MoveWindow(frame->content, 0, 0, client.right, client.bottom, TRUE);
      }

      static void queue_configure(struct libdecor_frame *frame)
      {
          if (frame->configure_pending || frame->hwnd == NULL) return;
          frame->configure_pending = true;
          PostMessageW(frame->hwnd, WM_LIBDECOR_CONFIGURE, 0, 0);
      }

      static void send_configure(struct libdecor_frame *frame)
      {
          struct libdecor_configuration configuration;
          RECT client;
          frame->configure_pending = false;
          if (frame->iface == NULL || frame->iface->configure == NULL) return;
          memset(&configuration, 0, sizeof(configuration));
          configuration.window_state = current_state(frame);
          /* Like the compositor's first configure, the initial one leaves the size to the client;
           * afterwards it carries the client area the user or the window manager chose. */
          if (frame->mapped && !IsIconic(frame->hwnd) && GetClientRect(frame->hwnd, &client) && client.right > 0 && client.bottom > 0) {
              configuration.has_size = frame->last_width != 0 || !libdecor_frame_is_floating(frame);
              configuration.width = client.right;
              configuration.height = client.bottom;
          }
          frame->last_state = configuration.window_state;
          libdecor_frame_ref(frame);
          frame->iface->configure(frame, &configuration, frame->user_data);
          libdecor_frame_unref(frame);
      }

      static struct libdecor_frame *frame_of(HWND hwnd)
      {
          return (struct libdecor_frame *)GetWindowLongPtrW(hwnd, GWLP_USERDATA);
      }

      static LRESULT CALLBACK frame_proc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
      {
          struct libdecor_frame *frame = frame_of(hwnd);
          if (message == WM_NCCREATE) {
              CREATESTRUCTW *create = (CREATESTRUCTW *)lparam;
              SetWindowLongPtrW(hwnd, GWLP_USERDATA, (LONG_PTR)create->lpCreateParams);
              return DefWindowProcW(hwnd, message, wparam, lparam);
          }
          if (frame == NULL) return DefWindowProcW(hwnd, message, wparam, lparam);
          switch (message) {
          case WM_LIBDECOR_CONFIGURE:
              send_configure(frame);
              return 0;
          case WM_SIZE:
              layout_content(frame);
              if (!frame->in_commit) queue_configure(frame);
              return 0;
          case WM_ACTIVATE:
              if (frame->content != NULL && LOWORD(wparam) != WA_INACTIVE) SetFocus(frame->content);
              queue_configure(frame);
              break;
          case WM_EXITSIZEMOVE:
              queue_configure(frame);
              break;
          case WM_GETMINMAXINFO: {
              MINMAXINFO *info = (MINMAXINFO *)lparam;
              int w, h;
              if (frame->min_width > 0 || frame->min_height > 0) {
                  outer_size(frame, frame->min_width, frame->min_height, &w, &h);
                  if (frame->min_width > 0) info->ptMinTrackSize.x = w;
                  if (frame->min_height > 0) info->ptMinTrackSize.y = h;
              }
              if (frame->max_width > 0 || frame->max_height > 0) {
                  outer_size(frame, frame->max_width, frame->max_height, &w, &h);
                  if (frame->max_width > 0) info->ptMaxTrackSize.x = w;
                  if (frame->max_height > 0) info->ptMaxTrackSize.y = h;
              }
              return 0;
          }
          case WM_SYSCOMMAND:
              switch (wparam & 0xfff0) {
              case SC_MOVE: if (!(frame->capabilities & LIBDECOR_ACTION_MOVE)) return 0; break;
              case SC_SIZE: if (!(frame->capabilities & LIBDECOR_ACTION_RESIZE)) return 0; break;
              case SC_MINIMIZE: if (!(frame->capabilities & LIBDECOR_ACTION_MINIMIZE)) return 0; break;
              case SC_CLOSE: if (!(frame->capabilities & LIBDECOR_ACTION_CLOSE)) return 0; break;
              }
              break;
          case WM_NCLBUTTONDOWN:
              if (wparam == HTCAPTION && !(frame->capabilities & LIBDECOR_ACTION_MOVE)) return 0;
              break;
          case WM_CLOSE:
              if (frame->iface != NULL && frame->iface->close != NULL) frame->iface->close(frame, frame->user_data);
              return 0;   /* the application decides (libdecor_frame_unref destroys the window) */
          case WM_CANCELMODE:
              if (frame->iface != NULL && frame->iface->dismiss_popup != NULL) frame->iface->dismiss_popup(frame, "seat0", frame->user_data);
              break;
          }
          return DefWindowProcW(hwnd, message, wparam, lparam);
      }

      static bool register_class(void)
      {
          static ATOM atom;
          WNDCLASSEXW wc;
          if (atom != 0) return true;
          memset(&wc, 0, sizeof(wc));
          wc.cbSize = sizeof(wc);
          wc.style = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS;
          wc.lpfnWndProc = frame_proc;
          wc.hInstance = GetModuleHandleW(NULL);
          wc.hCursor = LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
          wc.hIcon = LoadIconW(NULL, (LPCWSTR)IDI_APPLICATION);
          wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
          wc.lpszClassName = FRAME_CLASS;
          atom = RegisterClassExW(&wc);
          return atom != 0 || GetLastError() == ERROR_CLASS_ALREADY_EXISTS;
      }

      LIBDECOR_EXPORT struct libdecor *libdecor_new(struct wl_display *display, struct libdecor_interface *iface)
      {
          struct libdecor *context = calloc(1, sizeof(*context));
          (void)display;
          if (context == NULL) return NULL;
          context->ref = 1;
          context->iface = iface;
          if (!register_class()) {
              context->failed = true;
              if (iface != NULL && iface->error != NULL)
                  iface->error(context, LIBDECOR_ERROR_COMPOSITOR_INCOMPATIBLE, "could not register the frame window class");
          }
          return context;
      }

      LIBDECOR_EXPORT void libdecor_unref(struct libdecor *context)
      {
          if (context == NULL || --context->ref > 0) return;
          while (context->frames != NULL) {
              struct libdecor_frame *frame = context->frames;
              frame->ref = 1;
              libdecor_frame_unref(frame);
          }
          free(context);
      }

      LIBDECOR_EXPORT int libdecor_get_fd(struct libdecor *context)
      {
          (void)context;
          return -1;   /* window messages can't be polled as a file descriptor; use libdecor_dispatch */
      }

      LIBDECOR_EXPORT int libdecor_dispatch(struct libdecor *context, int timeout)
      {
          MSG msg;
          int dispatched = 0;
          if (context == NULL || context->failed) return -1;
          if (!PeekMessageW(&msg, NULL, 0, 0, PM_NOREMOVE)) {
              DWORD wait = MsgWaitForMultipleObjectsEx(0, NULL, timeout < 0 ? INFINITE : (DWORD)timeout, QS_ALLINPUT, MWMO_INPUTAVAILABLE);
              if (wait == WAIT_FAILED) return -1;
          }
          while (PeekMessageW(&msg, NULL, 0, 0, PM_REMOVE)) {
              if (msg.message == WM_QUIT) {
                  PostQuitMessage((int)msg.wParam);
                  break;
              }
              TranslateMessage(&msg);
              DispatchMessageW(&msg);
              dispatched++;
          }
          return dispatched;
      }

      LIBDECOR_EXPORT struct libdecor_frame *libdecor_decorate(struct libdecor *context, struct wl_surface *surface,
                                                               struct libdecor_frame_interface *iface, void *user_data)
      {
          struct libdecor_frame *frame;
          if (context == NULL || context->failed) return NULL;
          frame = calloc(1, sizeof(*frame));
          if (frame == NULL) return NULL;
          frame->ref = 1;
          frame->context = context;
          frame->iface = iface;
          frame->user_data = user_data;
          frame->visible = true;
          frame->capabilities = LIBDECOR_ACTION_MOVE | LIBDECOR_ACTION_RESIZE | LIBDECOR_ACTION_MINIMIZE |
                                LIBDECOR_ACTION_FULLSCREEN | LIBDECOR_ACTION_CLOSE;
          frame->hwnd = CreateWindowExW(WS_EX_APPWINDOW, FRAME_CLASS, L"", frame_style(frame),
                                        CW_USEDEFAULT, CW_USEDEFAULT, 640, 480, NULL, NULL, GetModuleHandleW(NULL), frame);
          if (frame->hwnd == NULL) {
              if (context->iface != NULL && context->iface->error != NULL)
                  context->iface->error(context, LIBDECOR_ERROR_INVALID_FRAME_CONFIGURATION, "could not create the frame window");
              free(frame);
              return NULL;
          }
          if (surface != NULL && IsWindow((HWND)surface)) {
              /* The application's content window becomes the frame's client area. */
              frame->content = (HWND)surface;
              SetWindowLongPtrW(frame->content, GWL_STYLE,
                                (GetWindowLongPtrW(frame->content, GWL_STYLE) & ~(LONG_PTR)(WS_POPUP | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU)) | WS_CHILD | WS_VISIBLE);
              SetParent(frame->content, frame->hwnd);
              layout_content(frame);
          }
          frame->next = context->frames;
          context->frames = frame;
          context->ref++;
          return frame;
      }

      LIBDECOR_EXPORT void libdecor_frame_ref(struct libdecor_frame *frame)
      {
          if (frame != NULL) frame->ref++;
      }

      LIBDECOR_EXPORT void libdecor_frame_unref(struct libdecor_frame *frame)
      {
          struct libdecor_frame **link;
          struct libdecor *context;
          if (frame == NULL || --frame->ref > 0) return;
          context = frame->context;
          for (link = &context->frames; *link != NULL; link = &(*link)->next)
              if (*link == frame) { *link = frame->next; break; }
          if (frame->content != NULL && IsWindow(frame->content)) {
              ShowWindow(frame->content, SW_HIDE);
              SetParent(frame->content, NULL);
          }
          if (frame->hwnd != NULL) {
              SetWindowLongPtrW(frame->hwnd, GWLP_USERDATA, 0);
              DestroyWindow(frame->hwnd);
          }
          free(frame->title);
          free(frame->app_id);
          free(frame);
          libdecor_unref(context);
      }

      LIBDECOR_EXPORT void *libdecor_frame_get_window(struct libdecor_frame *frame)
      {
          return frame != NULL ? (void *)frame->hwnd : NULL;
      }

      LIBDECOR_EXPORT void libdecor_frame_set_visibility(struct libdecor_frame *frame, bool visible)
      {
          if (frame == NULL || frame->visible == visible) return;
          frame->visible = visible;
          apply_style(frame);
          if (frame->iface != NULL && frame->iface->commit != NULL) frame->iface->commit(frame, frame->user_data);
      }

      LIBDECOR_EXPORT bool libdecor_frame_is_visible(struct libdecor_frame *frame)
      {
          return frame != NULL && frame->visible;
      }

      LIBDECOR_EXPORT void libdecor_frame_set_parent(struct libdecor_frame *frame, struct libdecor_frame *parent)
      {
          if (frame == NULL) return;
          /* A parent frame is the Windows owner: the window stays above it and minimizes with it. */
          SetWindowLongPtrW(frame->hwnd, GWLP_HWNDPARENT, (LONG_PTR)(parent != NULL ? parent->hwnd : NULL));
      }

      LIBDECOR_EXPORT void libdecor_frame_set_title(struct libdecor_frame *frame, const char *title)
      {
          wchar_t *wide;
          if (frame == NULL || title == NULL) return;
          free(frame->title);
          frame->title = dup_string(title);
          wide = widen(title);
          if (wide != NULL) {
              SetWindowTextW(frame->hwnd, wide);
              free(wide);
          }
      }

      LIBDECOR_EXPORT const char *libdecor_frame_get_title(struct libdecor_frame *frame)
      {
          return frame != NULL ? frame->title : NULL;
      }

      LIBDECOR_EXPORT void libdecor_frame_set_app_id(struct libdecor_frame *frame, const char *app_id)
      {
          /* The app id groups windows on the taskbar: the window's AppUserModelID property. */
          static const GUID iid_property_store = { 0x886d8eeb, 0x8cf2, 0x4446, { 0x8d, 0x02, 0xcd, 0xba, 0x1d, 0xbd, 0xcf, 0x99 } };
          static const struct { GUID fmtid; DWORD pid; } key_app_id =
              { { 0x9f4c2855, 0x9f79, 0x4b39, { 0xa8, 0xd0, 0xe1, 0xd4, 0x2d, 0xe1, 0xd5, 0xf3 } }, 5 };
          typedef struct store_vtbl {
              HRESULT (STDMETHODCALLTYPE *QueryInterface)(void *, REFIID, void **);
              ULONG (STDMETHODCALLTYPE *AddRef)(void *);
              ULONG (STDMETHODCALLTYPE *Release)(void *);
              HRESULT (STDMETHODCALLTYPE *GetCount)(void *, DWORD *);
              HRESULT (STDMETHODCALLTYPE *GetAt)(void *, DWORD, void *);
              HRESULT (STDMETHODCALLTYPE *GetValue)(void *, const void *, void *);
              HRESULT (STDMETHODCALLTYPE *SetValue)(void *, const void *, const void *);
              HRESULT (STDMETHODCALLTYPE *Commit)(void *);
          } store_vtbl;
          typedef HRESULT (WINAPI *get_store_fn)(HWND, REFIID, void **);
          struct { store_vtbl *vtbl; } *store = NULL;
          struct { unsigned short vt, r1, r2, r3; wchar_t *str; void *pad; } value;
          get_store_fn get_store;
          wchar_t *wide;
          if (frame == NULL || app_id == NULL) return;
          free(frame->app_id);
          frame->app_id = dup_string(app_id);
          get_store = (get_store_fn)(void *)GetProcAddress(LoadLibraryW(L"shell32.dll"), "SHGetPropertyStoreForWindow");
          if (get_store == NULL || FAILED(get_store(frame->hwnd, &iid_property_store, (void **)&store)) || store == NULL) return;
          wide = widen(app_id);
          if (wide != NULL) {
              memset(&value, 0, sizeof(value));
              value.vt = 31;   /* VT_LPWSTR */
              value.str = wide;
              store->vtbl->SetValue(store, &key_app_id, &value);
              store->vtbl->Commit(store);
              free(wide);
          }
          store->vtbl->Release(store);
      }

      LIBDECOR_EXPORT void libdecor_frame_set_capabilities(struct libdecor_frame *frame, enum libdecor_capabilities capabilities)
      {
          if (frame == NULL) return;
          frame->capabilities |= capabilities;
          apply_style(frame);
      }

      LIBDECOR_EXPORT void libdecor_frame_unset_capabilities(struct libdecor_frame *frame, enum libdecor_capabilities capabilities)
      {
          if (frame == NULL) return;
          frame->capabilities &= ~capabilities;
          apply_style(frame);
      }

      LIBDECOR_EXPORT bool libdecor_frame_has_capability(struct libdecor_frame *frame, enum libdecor_capabilities capability)
      {
          return frame != NULL && (frame->capabilities & capability) != 0;
      }

      LIBDECOR_EXPORT void libdecor_frame_show_window_menu(struct libdecor_frame *frame, struct wl_seat *wl_seat,
                                                           uint32_t serial, int x, int y)
      {
          POINT pt;
          HMENU menu;
          UINT command;
          (void)wl_seat; (void)serial;
          if (frame == NULL || (menu = GetSystemMenu(frame->hwnd, FALSE)) == NULL) return;
          pt.x = x;
          pt.y = y;
          ClientToScreen(frame->content != NULL ? frame->content : frame->hwnd, &pt);
          command = (UINT)TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.x, pt.y, 0, frame->hwnd, NULL);
          if (command != 0) PostMessageW(frame->hwnd, WM_SYSCOMMAND, command, 0);
      }

      LIBDECOR_EXPORT void libdecor_frame_popup_grab(struct libdecor_frame *frame, const char *seat_name)
      {
          (void)seat_name;
          if (frame != NULL && frame->content != NULL) SetCapture(frame->content);
      }

      LIBDECOR_EXPORT void libdecor_frame_popup_ungrab(struct libdecor_frame *frame, const char *seat_name)
      {
          (void)frame; (void)seat_name;
          ReleaseCapture();
      }

      LIBDECOR_EXPORT void libdecor_frame_translate_coordinate(struct libdecor_frame *frame, int surface_x, int surface_y,
                                                               int *frame_x, int *frame_y)
      {
          POINT origin = { 0, 0 };
          RECT window;
          if (frame == NULL || !GetWindowRect(frame->hwnd, &window)) {
              if (frame_x) *frame_x = surface_x;
              if (frame_y) *frame_y = surface_y;
              return;
          }
          ClientToScreen(frame->hwnd, &origin);
          if (frame_x) *frame_x = surface_x + (origin.x - window.left);
          if (frame_y) *frame_y = surface_y + (origin.y - window.top);
      }

      LIBDECOR_EXPORT void libdecor_frame_set_min_content_size(struct libdecor_frame *frame, int content_width, int content_height)
      {
          if (frame == NULL) return;
          frame->min_width = content_width;
          frame->min_height = content_height;
      }

      LIBDECOR_EXPORT void libdecor_frame_set_max_content_size(struct libdecor_frame *frame, int content_width, int content_height)
      {
          if (frame == NULL) return;
          frame->max_width = content_width;
          frame->max_height = content_height;
      }

      LIBDECOR_EXPORT void libdecor_frame_get_min_content_size(const struct libdecor_frame *frame, int *content_width, int *content_height)
      {
          if (content_width) *content_width = frame != NULL ? frame->min_width : 0;
          if (content_height) *content_height = frame != NULL ? frame->min_height : 0;
      }

      LIBDECOR_EXPORT void libdecor_frame_get_max_content_size(const struct libdecor_frame *frame, int *content_width, int *content_height)
      {
          if (content_width) *content_width = frame != NULL ? frame->max_width : 0;
          if (content_height) *content_height = frame != NULL ? frame->max_height : 0;
      }

      LIBDECOR_EXPORT void libdecor_frame_resize(struct libdecor_frame *frame, struct wl_seat *wl_seat, uint32_t serial,
                                                 enum libdecor_resize_edge edge)
      {
          static const WPARAM hits[] = { HTNOWHERE, HTTOP, HTBOTTOM, HTLEFT, HTTOPLEFT, HTBOTTOMLEFT, HTRIGHT, HTTOPRIGHT, HTBOTTOMRIGHT };
          POINT pt;
          (void)wl_seat; (void)serial;
          if (frame == NULL || edge <= LIBDECOR_RESIZE_EDGE_NONE || edge > LIBDECOR_RESIZE_EDGE_BOTTOM_RIGHT) return;
          if (!(frame->capabilities & LIBDECOR_ACTION_RESIZE)) return;
          /* An interactive resize from that edge, as if the user grabbed the border. */
          GetCursorPos(&pt);
          ReleaseCapture();
          SendMessageW(frame->hwnd, WM_NCLBUTTONDOWN, hits[edge], MAKELPARAM(pt.x, pt.y));
      }

      LIBDECOR_EXPORT void libdecor_frame_move(struct libdecor_frame *frame, struct wl_seat *wl_seat, uint32_t serial)
      {
          POINT pt;
          (void)wl_seat; (void)serial;
          if (frame == NULL || !(frame->capabilities & LIBDECOR_ACTION_MOVE)) return;
          GetCursorPos(&pt);
          ReleaseCapture();
          SendMessageW(frame->hwnd, WM_NCLBUTTONDOWN, HTCAPTION, MAKELPARAM(pt.x, pt.y));
      }

      LIBDECOR_EXPORT void libdecor_frame_commit(struct libdecor_frame *frame, struct libdecor_state *state,
                                                 struct libdecor_configuration *configuration)
      {
          (void)configuration;
          if (frame == NULL || state == NULL) return;
          /* A floating window takes the size the client chose; tiled, maximized and fullscreen ones
           * keep the size the window manager gave them. */
          if (libdecor_frame_is_floating(frame) && !IsIconic(frame->hwnd) && state->width > 0 && state->height > 0) {
              RECT client;
              if (GetClientRect(frame->hwnd, &client) && (client.right != state->width || client.bottom != state->height)) {
                  int w, h;
                  outer_size(frame, state->width, state->height, &w, &h);
                  frame->in_commit = true;
                  SetWindowPos(frame->hwnd, NULL, 0, 0, w, h, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
                  frame->in_commit = false;
              }
          }
          frame->last_width = state->width;
          frame->last_height = state->height;
      }

      LIBDECOR_EXPORT void libdecor_frame_set_minimized(struct libdecor_frame *frame)
      {
          if (frame != NULL) ShowWindow(frame->hwnd, SW_MINIMIZE);
      }

      LIBDECOR_EXPORT void libdecor_frame_set_maximized(struct libdecor_frame *frame)
      {
          if (frame != NULL && !frame->fullscreen) ShowWindow(frame->hwnd, SW_MAXIMIZE);
      }

      LIBDECOR_EXPORT void libdecor_frame_unset_maximized(struct libdecor_frame *frame)
      {
          if (frame != NULL && IsZoomed(frame->hwnd)) ShowWindow(frame->hwnd, SW_RESTORE);
      }

      LIBDECOR_EXPORT void libdecor_frame_set_fullscreen(struct libdecor_frame *frame, struct wl_output *output)
      {
          MONITORINFO info = { sizeof(info) };
          (void)output;
          if (frame == NULL || frame->fullscreen) return;
          /* Borderless, covering the monitor the window is on. */
          frame->restore.length = sizeof(frame->restore);
          GetWindowPlacement(frame->hwnd, &frame->restore);
          frame->fullscreen = true;
          apply_style(frame);
          if (GetMonitorInfoW(MonitorFromWindow(frame->hwnd, MONITOR_DEFAULTTONEAREST), &info))
              SetWindowPos(frame->hwnd, HWND_TOP, info.rcMonitor.left, info.rcMonitor.top,
                           info.rcMonitor.right - info.rcMonitor.left, info.rcMonitor.bottom - info.rcMonitor.top,
                           SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
          queue_configure(frame);
      }

      LIBDECOR_EXPORT void libdecor_frame_unset_fullscreen(struct libdecor_frame *frame)
      {
          if (frame == NULL || !frame->fullscreen) return;
          frame->fullscreen = false;
          apply_style(frame);
          SetWindowPlacement(frame->hwnd, &frame->restore);
          SetWindowPos(frame->hwnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
          queue_configure(frame);
      }

      LIBDECOR_EXPORT bool libdecor_frame_is_floating(struct libdecor_frame *frame)
      {
          enum libdecor_window_state state;
          if (frame == NULL) return false;
          state = current_state(frame);
          return !(state & (LIBDECOR_WINDOW_STATE_FULLSCREEN | LIBDECOR_WINDOW_STATE_MAXIMIZED |
                            LIBDECOR_WINDOW_STATE_TILED_LEFT | LIBDECOR_WINDOW_STATE_TILED_RIGHT |
                            LIBDECOR_WINDOW_STATE_TILED_TOP | LIBDECOR_WINDOW_STATE_TILED_BOTTOM));
      }

      LIBDECOR_EXPORT void libdecor_frame_close(struct libdecor_frame *frame)
      {
          /* Like xdg_toplevel's close: ask the application through its close callback. */
          if (frame != NULL && frame->iface != NULL && frame->iface->close != NULL) frame->iface->close(frame, frame->user_data);
      }

      LIBDECOR_EXPORT void libdecor_frame_map(struct libdecor_frame *frame)
      {
          if (frame == NULL) return;
          frame->mapped = true;
          ShowWindow(frame->hwnd, SW_SHOWNORMAL);
          UpdateWindow(frame->hwnd);
          queue_configure(frame);
      }

      LIBDECOR_EXPORT struct xdg_surface *libdecor_frame_get_xdg_surface(struct libdecor_frame *frame)
      {
          (void)frame;
          return NULL;   /* no xdg-shell: the frame is a native window (libdecor_frame_get_window) */
      }

      LIBDECOR_EXPORT struct xdg_toplevel *libdecor_frame_get_xdg_toplevel(struct libdecor_frame *frame)
      {
          (void)frame;
          return NULL;
      }

      LIBDECOR_EXPORT struct libdecor_state *libdecor_state_new(int width, int height)
      {
          struct libdecor_state *state = calloc(1, sizeof(*state));
          if (state == NULL) return NULL;
          state->width = width;
          state->height = height;
          return state;
      }

      LIBDECOR_EXPORT void libdecor_state_free(struct libdecor_state *state)
      {
          free(state);
      }

      LIBDECOR_EXPORT bool libdecor_configuration_get_content_size(struct libdecor_configuration *configuration,
                                                                   struct libdecor_frame *frame, int *width, int *height)
      {
          (void)frame;
          if (configuration == NULL || !configuration->has_size) return false;
          if (width) *width = configuration->width;
          if (height) *height = configuration->height;
          return true;
      }

      LIBDECOR_EXPORT bool libdecor_configuration_get_window_state(struct libdecor_configuration *configuration,
                                                                   enum libdecor_window_state *window_state)
      {
          if (configuration == NULL) return false;
          if (window_state) *window_state = configuration->window_state;
          return true;
      }
    C

    lib.mkpath
    bin.mkpath
    system ENV.cc, "-O2", "-shared", "-Iwin", "-o", bin/"libdecor-0.dll", "win/libdecor.c",
           "-Wl,--out-implib,#{lib}/libdecor-0.dll.a", "-luser32"
    (include/"libdecor-0").install "win/libdecor.h"
    (lib/"pkgconfig/libdecor-0.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: libdecor
      Description: Window decoration library (Win32 frames)
      Version: #{version}
      Cflags: -I${includedir}/libdecor-0
      Libs: -L${libdir} -ldecor-0
    PC
    doc.install "README.md", "LICENSE"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <libdecor.h>

      int main() {
        struct libdecor *context;
        return 0;
      }
    C
    system ENV.cc, "test.c", "-o", "test", "-I#{include}/libdecor-0"
    system "./test"

    # A frame that is never mapped: its window exists but is not shown.
    (testpath/"frame.c").write <<~C
      #include <libdecor.h>
      #include <stdio.h>
      #include <string.h>

      static void configure(struct libdecor_frame *frame, struct libdecor_configuration *configuration, void *data) {}
      static void close_frame(struct libdecor_frame *frame, void *data) {}
      static void commit(struct libdecor_frame *frame, void *data) {}
      static struct libdecor_interface iface = { NULL };
      static struct libdecor_frame_interface frame_iface = { configure, close_frame, commit };

      int main() {
        struct libdecor *context = libdecor_new(NULL, &iface);
        struct libdecor_frame *frame = libdecor_decorate(context, NULL, &frame_iface, NULL);
        if (frame == NULL) return 1;
        libdecor_frame_set_title(frame, "brew test");
        libdecor_frame_set_min_content_size(frame, 200, 100);
        libdecor_frame_unset_capabilities(frame, LIBDECOR_ACTION_RESIZE);
        int w, h;
        libdecor_frame_get_min_content_size(frame, &w, &h);
        printf("%s %dx%d resize=%d window=%d\\n", libdecor_frame_get_title(frame), w, h,
               libdecor_frame_has_capability(frame, LIBDECOR_ACTION_RESIZE), libdecor_frame_get_window(frame) != NULL);
        libdecor_dispatch(context, 0);
        libdecor_frame_unref(frame);
        libdecor_unref(context);
        return 0;
      }
    C
    system ENV.cc, "frame.c", "-o", "frame.exe", "-I#{include}/libdecor-0", "-L#{lib}", "-ldecor-0"
    cp bin/"libdecor-0.dll", testpath
    assert_equal "brew test 200x100 resize=0 window=1", shell_output("./frame.exe").strip
  end
end
