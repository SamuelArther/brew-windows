class Mmtabbarview < Formula
  desc "Safari-style tab bar control, as a native Win32 control"
  homepage "https://mimo42.github.io/MMTabBarView/"
  url "https://github.com/MiMo42/MMTabBarView/archive/refs/tags/v1.4.2.tar.gz"
  sha256 "a5b79f1b50f6cabe97558f4c24a6317c448c534f15655309b6b29a532590e976"
  license "BSD-3-Clause"

  # MMTabBarView is a Cocoa framework (an NSView subclass), and Cocoa does not exist on Windows.
  # Windows' own tab control (comctl32) lacks what MMTabBarView adds: close buttons, an add-tab
  # button, an overflow menu, drag reordering, edited and count badges, and veto-able close
  # notifications. This formula provides that tab bar for Windows programs as a header-only Win32
  # control (window class "MMTabBarView", mmtb_* functions, WM_NOTIFY in place of the delegate),
  # at the framework's include path: #include <MMTabBarView/MMTabBarView.h>, link user32 and gdi32.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    (include/"MMTabBarView/MMTabBarView.h").write <<~'C'
      /* MMTabBarView for Windows (brew-windows' mmtabbarview formula)
       *
       * MMTabBarView is a Cocoa tab bar: Safari-style tabs with close buttons, an add-tab button, an
       * overflow menu, drag reordering, edited/count badges and delegate callbacks. Cocoa does not exist
       * on Windows, so this header implements the same tab bar as a native Win32 control (window class
       * "MMTabBarView", GDI drawing in the system colours). It is header-only: include it, then create
       * the control with mmtb_create() or CreateWindowExW(0, MMTB_CLASS, ...). Link user32 and gdi32.
       *
       *   tab bar view                      mmtb_create, mmtb_set_options, mmtb_set_button_widths
       *   tabs (NSTabViewItem)              mmtb_add_tab / insert / remove / move / select, labels,
       *                                     icons, user data, isEdited dot, objectCount badge
       *   delegate                          WM_NOTIFY to the parent with an NMMMTABBAR:
       *     tabView:shouldCloseTabViewItem:   MMTBN_SHOULDCLOSE (return nonzero to keep the tab)
       *     tabView:didCloseTabViewItem:      MMTBN_DIDCLOSE (sent before the tab is removed)
       *     tabView:didSelectTabViewItem:     MMTBN_DIDSELECT
       *     tabView:didMoveTabViewItem:...    MMTBN_DIDMOVE (index -> to_index)
       *     addNewTabToTabView:               MMTBN_ADDTAB
       *
       * Mouse: click selects, drag reorders, the close button or a middle click closes, the + button
       * asks for a new tab, the >> button lists tabs that do not fit. */

      #ifndef MMTABBARVIEW_H
      #define MMTABBARVIEW_H

      #include <windows.h>
      #include <windowsx.h>
      #include <stdlib.h>
      #include <string.h>
      #include <wchar.h>

      #ifdef __cplusplus
      extern "C" {
      #endif

      #define MMTB_CLASS L"MMTabBarView"

      /* mmtb_set_options flags (MMTabBarView properties). */
      #define MMTB_ONLY_SHOW_CLOSE_ON_HOVER 0x0001  /* onlyShowCloseOnHover */
      #define MMTB_DISABLE_TAB_CLOSE        0x0002  /* disableTabClose */
      #define MMTB_CAN_CLOSE_ONLY_TAB       0x0004  /* canCloseOnlyTab */
      #define MMTB_HIDE_FOR_SINGLE_TAB      0x0008  /* hideForSingleTab: draws nothing with one tab */
      #define MMTB_SHOW_ADD_TAB_BUTTON      0x0010  /* showAddTabButton */
      #define MMTB_USE_OVERFLOW_MENU        0x0020  /* useOverflowMenu */
      #define MMTB_SIZE_BUTTONS_TO_FIT      0x0040  /* sizeButtonsToFit: share the width evenly */
      #define MMTB_ALLOW_BACKGROUND_CLOSING 0x0080  /* allowsBackgroundTabClosing */
      #define MMTB_DISABLE_DRAG             0x0100

      /* WM_NOTIFY codes sent to the parent. */
      #define MMTBN_FIRST       (0U - 1700U)
      #define MMTBN_SHOULDCLOSE (MMTBN_FIRST - 0)
      #define MMTBN_DIDCLOSE    (MMTBN_FIRST - 1)
      #define MMTBN_DIDSELECT   (MMTBN_FIRST - 2)
      #define MMTBN_DIDMOVE     (MMTBN_FIRST - 3)
      #define MMTBN_ADDTAB      (MMTBN_FIRST - 4)

      typedef struct NMMMTABBAR {
        NMHDR hdr;
        int index;      /* the tab concerned (-1 for MMTBN_ADDTAB) */
        int to_index;   /* MMTBN_DIDMOVE: where it went */
        LPARAM data;    /* the tab's user data */
      } NMMMTABBAR;

      typedef struct MMTabBarTab_ {
        wchar_t *label;
        LPARAM data;
        HICON icon;
        BOOL edited;
        int object_count;
        int x, width;   /* layout, client coordinates; width 0 = in the overflow menu */
      } MMTabBarTab_;

      typedef struct MMTabBarState_ {
        MMTabBarTab_ *tabs;
        int count, capacity, selected, hot, hot_close, pressed_close, drag_from;
        BOOL dragging, tracking;
        int press_x;
        UINT options;
        int min_width, max_width, optimum_width;
        HFONT font;
        RECT add_button, overflow_button;
      } MMTabBarState_;

      /* ---- internals ---- */

      static inline MMTabBarState_ *mmtb_state_(HWND bar)
      {
        return (MMTabBarState_ *) GetWindowLongPtrW(bar, GWLP_USERDATA);
      }

      static inline int mmtb_scale_(HWND bar, int value)
      {
        HDC dc = GetDC(bar);
        int dpi = dc ? GetDeviceCaps(dc, LOGPIXELSX) : 96;
        if (dc) ReleaseDC(bar, dc);
        return MulDiv(value, dpi, 96);
      }

      static inline LRESULT mmtb_notify_(HWND bar, UINT code, int index, int to_index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        NMMMTABBAR nm;
        memset(&nm, 0, sizeof nm);
        nm.hdr.hwndFrom = bar;
        nm.hdr.idFrom = (UINT_PTR) GetDlgCtrlID(bar);
        nm.hdr.code = code;
        nm.index = index;
        nm.to_index = to_index;
        nm.data = s && index >= 0 && index < s->count ? s->tabs[index].data : 0;
        return SendMessageW(GetParent(bar), WM_NOTIFY, nm.hdr.idFrom, (LPARAM) &nm);
      }

      static inline void mmtb_layout_(HWND bar)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        RECT rc;
        int i, button = mmtb_scale_(bar, 24), avail, width, visible, x = 0;
        GetClientRect(bar, &rc);
        SetRectEmpty(&s->add_button);
        SetRectEmpty(&s->overflow_button);
        avail = rc.right - ((s->options & MMTB_SHOW_ADD_TAB_BUTTON) ? button : 0);
        if (s->count == 0) visible = 0;
        else if (s->options & MMTB_SIZE_BUTTONS_TO_FIT)
          {
            visible = s->count;
            width = avail / s->count;
            if (width > s->max_width) width = s->max_width;
            if (width < s->min_width && (s->options & MMTB_USE_OVERFLOW_MENU))
              {
                visible = (avail - button) / s->min_width;
                width = s->min_width;
              }
          }
        else
          {
            width = s->optimum_width;
            visible = s->count;
            if (width * s->count > avail)
              {
                width = avail / s->count;
                if (width < s->min_width)
                  {
                    width = s->min_width;
                    if (s->options & MMTB_USE_OVERFLOW_MENU) visible = (avail - button) / s->min_width;
                  }
              }
          }
        if (visible < 1 && s->count > 0) visible = 1;
        if (visible > s->count) visible = s->count;
        /* The selected tab always stays on the bar; it takes the last slot when it would overflow. */
        for (i = 0; i < s->count; i++)
          {
            BOOL shown = s->selected < visible ? i < visible : (i < visible - 1 || i == s->selected);
            s->tabs[i].x = shown ? x : 0;
            s->tabs[i].width = shown ? width : 0;
            if (shown) x += width;
          }
        if (visible < s->count)
          SetRect(&s->overflow_button, x, 0, x + button, rc.bottom), x += button;
        if (s->options & MMTB_SHOW_ADD_TAB_BUTTON)
          SetRect(&s->add_button, x, 0, x + button, rc.bottom);
      }

      static inline BOOL mmtb_closable_(MMTabBarState_ *s, int i)
      {
        (void) i;
        if (s->options & MMTB_DISABLE_TAB_CLOSE) return FALSE;
        if (s->count == 1 && !(s->options & MMTB_CAN_CLOSE_ONLY_TAB)) return FALSE;
        return TRUE;
      }

      static inline RECT mmtb_close_rect_(HWND bar, MMTabBarState_ *s, int i)
      {
        RECT rc, r;
        int size = mmtb_scale_(bar, 14), pad = mmtb_scale_(bar, 6);
        GetClientRect(bar, &rc);
        r.left = s->tabs[i].x + pad;
        r.top = (rc.bottom - size) / 2;
        r.right = r.left + size;
        r.bottom = r.top + size;
        return r;
      }

      static inline int mmtb_hit_(HWND bar, int x, int y, BOOL *on_close)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        int i;
        *on_close = FALSE;
        for (i = 0; i < s->count; i++)
          if (s->tabs[i].width > 0 && x >= s->tabs[i].x && x < s->tabs[i].x + s->tabs[i].width)
            {
              RECT close = mmtb_close_rect_(bar, s, i);
              POINT pt;
              pt.x = x;
              pt.y = y;
              *on_close = mmtb_closable_(s, i) && PtInRect(&close, pt);
              return i;
            }
        return -1;
      }

      static inline void mmtb_paint_(HWND bar, HDC target)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        RECT rc, r;
        HDC dc;
        HBITMAP bitmap, old_bitmap;
        HFONT old_font;
        HPEN pen, old_pen;
        int i, line = mmtb_scale_(bar, 2), pad = mmtb_scale_(bar, 6);
        GetClientRect(bar, &rc);
        dc = CreateCompatibleDC(target);
        bitmap = CreateCompatibleBitmap(target, rc.right > 0 ? rc.right : 1, rc.bottom > 0 ? rc.bottom : 1);
        old_bitmap = (HBITMAP) SelectObject(dc, bitmap);
        old_font = (HFONT) SelectObject(dc, s->font ? s->font : GetStockObject(DEFAULT_GUI_FONT));
        FillRect(dc, &rc, GetSysColorBrush(COLOR_BTNFACE));
        SetBkMode(dc, TRANSPARENT);
        pen = CreatePen(PS_SOLID, 1, GetSysColor(COLOR_BTNSHADOW));
        old_pen = (HPEN) SelectObject(dc, pen);
        if (!(s->count == 1 && (s->options & MMTB_HIDE_FOR_SINGLE_TAB)))
          for (i = 0; i < s->count; i++)
            {
              MMTabBarTab_ *t = &s->tabs[i];
              BOOL selected = i == s->selected, hot = i == s->hot;
              RECT close = mmtb_close_rect_(bar, s, i);
              int text_left;
              if (t->width == 0) continue;
              SetRect(&r, t->x, 0, t->x + t->width, rc.bottom);
              if (selected || hot)
                FillRect(dc, &r, GetSysColorBrush(selected ? COLOR_WINDOW : COLOR_3DLIGHT));
              if (selected)
                {
                  RECT accent = r;
                  accent.bottom = accent.top + line;
                  FillRect(dc, &accent, GetSysColorBrush(COLOR_HIGHLIGHT));
                }
              /* separator on the right edge */
              MoveToEx(dc, r.right - 1, r.top + pad, NULL);
              LineTo(dc, r.right - 1, r.bottom - pad);
              text_left = close.right + pad;
              /* close button, or the edited dot in its place */
              if (mmtb_closable_(s, i) && ((selected || hot) || !(s->options & MMTB_ONLY_SHOW_CLOSE_ON_HOVER))
                  && (selected || hot || (s->options & MMTB_ALLOW_BACKGROUND_CLOSING) || !(s->options & MMTB_ONLY_SHOW_CLOSE_ON_HOVER))
                  && !(t->edited && !(hot && s->hot_close)))
                {
                  HPEN xpen = CreatePen(PS_SOLID, line, GetSysColor(i == s->hot && s->hot_close ? COLOR_HIGHLIGHT : COLOR_BTNTEXT));
                  HPEN prev = (HPEN) SelectObject(dc, xpen);
                  InflateRect(&close, -line - 1, -line - 1);
                  MoveToEx(dc, close.left, close.top, NULL);
                  LineTo(dc, close.right, close.bottom);
                  MoveToEx(dc, close.right, close.top, NULL);
                  LineTo(dc, close.left, close.bottom);
                  SelectObject(dc, prev);
                  DeleteObject(xpen);
                }
              else if (t->edited)
                {
                  HBRUSH dot = CreateSolidBrush(GetSysColor(COLOR_BTNTEXT));
                  HBRUSH prev = (HBRUSH) SelectObject(dc, dot);
                  HPEN none = (HPEN) SelectObject(dc, GetStockObject(NULL_PEN));
                  InflateRect(&close, -mmtb_scale_(bar, 3), -mmtb_scale_(bar, 3));
                  Ellipse(dc, close.left, close.top, close.right, close.bottom);
                  SelectObject(dc, none);
                  SelectObject(dc, prev);
                  DeleteObject(dot);
                }
              if (t->icon)
                {
                  int size = GetSystemMetrics(SM_CXSMICON);
                  DrawIconEx(dc, text_left, (rc.bottom - size) / 2, t->icon, size, size, 0, NULL, DI_NORMAL);
                  text_left += size + pad / 2;
                }
              SetRect(&r, text_left, 0, t->x + t->width - pad, rc.bottom);
              if (t->object_count > 0)
                {
                  wchar_t count[16];
                  SIZE extent;
                  RECT badge;
                  HBRUSH fill = GetSysColorBrush(COLOR_BTNSHADOW);
                  HBRUSH prev = (HBRUSH) SelectObject(dc, fill);
                  HPEN none = (HPEN) SelectObject(dc, GetStockObject(NULL_PEN));
                  swprintf(count, 16, L"%d", t->object_count);
                  GetTextExtentPoint32W(dc, count, (int) wcslen(count), &extent);
                  SetRect(&badge, r.right - extent.cx - pad * 2, (rc.bottom - extent.cy) / 2 - 1,
                          r.right, (rc.bottom + extent.cy) / 2 + 1);
                  RoundRect(dc, badge.left, badge.top, badge.right, badge.bottom, extent.cy, extent.cy);
                  SetTextColor(dc, GetSysColor(COLOR_WINDOW));
                  DrawTextW(dc, count, -1, &badge, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
                  SelectObject(dc, none);
                  SelectObject(dc, prev);
                  r.right = badge.left - pad / 2;
                }
              SetTextColor(dc, GetSysColor(COLOR_BTNTEXT));
              DrawTextW(dc, t->label ? t->label : L"", -1, &r, DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS | DT_NOPREFIX);
            }
        SetTextColor(dc, GetSysColor(COLOR_BTNTEXT));
        if (!IsRectEmpty(&s->overflow_button))
          DrawTextW(dc, L"\x00BB", -1, &s->overflow_button, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
        if (!IsRectEmpty(&s->add_button))
          DrawTextW(dc, L"+", -1, &s->add_button, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
        /* bottom border */
        MoveToEx(dc, 0, rc.bottom - 1, NULL);
        LineTo(dc, rc.right, rc.bottom - 1);
        BitBlt(target, 0, 0, rc.right, rc.bottom, dc, 0, 0, SRCCOPY);
        SelectObject(dc, old_pen);
        DeleteObject(pen);
        SelectObject(dc, old_font);
        SelectObject(dc, old_bitmap);
        DeleteObject(bitmap);
        DeleteDC(dc);
      }

      static inline void mmtb_changed_(HWND bar)
      {
        mmtb_layout_(bar);
        InvalidateRect(bar, NULL, FALSE);
      }

      /* ---- public API ---- */

      static inline int mmtb_tab_count(HWND bar) { return mmtb_state_(bar)->count; }
      static inline int mmtb_selected(HWND bar) { return mmtb_state_(bar)->selected; }

      static inline LPARAM mmtb_tab_data(HWND bar, int index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        return index >= 0 && index < s->count ? s->tabs[index].data : 0;
      }

      static inline const wchar_t *mmtb_tab_label(HWND bar, int index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        return index >= 0 && index < s->count ? s->tabs[index].label : NULL;
      }

      static inline int mmtb_insert_tab(HWND bar, int index, const wchar_t *label, LPARAM data)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index < 0 || index > s->count) index = s->count;
        if (s->count == s->capacity)
          {
            int capacity = s->capacity ? s->capacity * 2 : 8;
            MMTabBarTab_ *tabs = (MMTabBarTab_ *) realloc(s->tabs, sizeof *tabs * capacity);
            if (!tabs) return -1;
            s->tabs = tabs;
            s->capacity = capacity;
          }
        memmove(&s->tabs[index + 1], &s->tabs[index], sizeof *s->tabs * (s->count - index));
        memset(&s->tabs[index], 0, sizeof *s->tabs);
        s->tabs[index].label = _wcsdup(label ? label : L"");
        s->tabs[index].data = data;
        s->count++;
        if (s->selected >= index && s->count > 1) s->selected++;
        if (s->selected < 0) s->selected = 0;
        mmtb_changed_(bar);
        return index;
      }

      static inline int mmtb_add_tab(HWND bar, const wchar_t *label, LPARAM data)
      {
        return mmtb_insert_tab(bar, -1, label, data);
      }

      static inline BOOL mmtb_select(HWND bar, int index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index < 0 || index >= s->count) return FALSE;
        if (index != s->selected)
          {
            s->selected = index;
            mmtb_changed_(bar);
            mmtb_notify_(bar, MMTBN_DIDSELECT, index, index);
          }
        return TRUE;
      }

      static inline BOOL mmtb_remove_tab(HWND bar, int index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        BOOL was_selected;
        if (index < 0 || index >= s->count) return FALSE;
        was_selected = index == s->selected;
        free(s->tabs[index].label);
        memmove(&s->tabs[index], &s->tabs[index + 1], sizeof *s->tabs * (s->count - index - 1));
        s->count--;
        if (s->selected > index || (s->selected == s->count && s->count > 0)) s->selected--;
        if (s->count == 0) s->selected = -1;
        s->hot = -1;
        mmtb_changed_(bar);
        if (was_selected && s->selected >= 0) mmtb_notify_(bar, MMTBN_DIDSELECT, s->selected, s->selected);
        return TRUE;
      }

      /* Asks the parent (MMTBN_SHOULDCLOSE), then tells it (MMTBN_DIDCLOSE) and removes the tab. */
      static inline BOOL mmtb_close_tab(HWND bar, int index)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index < 0 || index >= s->count || !mmtb_closable_(s, index)) return FALSE;
        if (mmtb_notify_(bar, MMTBN_SHOULDCLOSE, index, index)) return FALSE;
        mmtb_notify_(bar, MMTBN_DIDCLOSE, index, index);
        return mmtb_remove_tab(bar, index);
      }

      static inline BOOL mmtb_move_tab(HWND bar, int from, int to)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        MMTabBarTab_ tab;
        if (from < 0 || from >= s->count || to < 0 || to >= s->count) return FALSE;
        if (from == to) return TRUE;
        tab = s->tabs[from];
        if (from < to) memmove(&s->tabs[from], &s->tabs[from + 1], sizeof tab * (to - from));
        else memmove(&s->tabs[to + 1], &s->tabs[to], sizeof tab * (from - to));
        s->tabs[to] = tab;
        if (s->selected == from) s->selected = to;
        else if (from < s->selected && to >= s->selected) s->selected--;
        else if (from > s->selected && to <= s->selected) s->selected++;
        mmtb_changed_(bar);
        return TRUE;
      }

      static inline BOOL mmtb_set_label(HWND bar, int index, const wchar_t *label)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index < 0 || index >= s->count) return FALSE;
        free(s->tabs[index].label);
        s->tabs[index].label = _wcsdup(label ? label : L"");
        mmtb_changed_(bar);
        return TRUE;
      }

      static inline void mmtb_set_icon(HWND bar, int index, HICON icon)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index >= 0 && index < s->count) { s->tabs[index].icon = icon; mmtb_changed_(bar); }
      }

      static inline void mmtb_set_edited(HWND bar, int index, BOOL edited)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index >= 0 && index < s->count) { s->tabs[index].edited = edited; mmtb_changed_(bar); }
      }

      static inline void mmtb_set_object_count(HWND bar, int index, int count)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        if (index >= 0 && index < s->count) { s->tabs[index].object_count = count; mmtb_changed_(bar); }
      }

      static inline void mmtb_set_options(HWND bar, UINT options)
      {
        mmtb_state_(bar)->options = options;
        mmtb_changed_(bar);
      }

      static inline UINT mmtb_options(HWND bar) { return mmtb_state_(bar)->options; }

      /* buttonMinWidth / buttonMaxWidth / buttonOptimumWidth, in pixels at 96 dpi. */
      static inline void mmtb_set_button_widths(HWND bar, int min_width, int max_width, int optimum_width)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        s->min_width = mmtb_scale_(bar, min_width);
        s->max_width = mmtb_scale_(bar, max_width);
        s->optimum_width = mmtb_scale_(bar, optimum_width);
        mmtb_changed_(bar);
      }

      static inline void mmtb_overflow_menu_(HWND bar)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        HMENU menu = CreatePopupMenu();
        POINT pt;
        int i, choice;
        for (i = 0; i < s->count; i++)
          AppendMenuW(menu, MF_STRING | (i == s->selected ? MF_CHECKED : 0), (UINT_PTR) i + 1, s->tabs[i].label);
        pt.x = s->overflow_button.left;
        pt.y = s->overflow_button.bottom;
        ClientToScreen(bar, &pt);
        choice = (int) TrackPopupMenu(menu, TPM_RETURNCMD | TPM_NONOTIFY, pt.x, pt.y, 0, bar, NULL);
        DestroyMenu(menu);
        if (choice > 0) mmtb_select(bar, choice - 1);
      }

      static inline LRESULT CALLBACK mmtb_wndproc_(HWND bar, UINT message, WPARAM wparam, LPARAM lparam)
      {
        MMTabBarState_ *s = mmtb_state_(bar);
        switch (message)
          {
          case WM_NCCREATE:
            s = (MMTabBarState_ *) calloc(1, sizeof *s);
            if (!s) return FALSE;
            s->selected = s->hot = s->drag_from = -1;
            SetWindowLongPtrW(bar, GWLP_USERDATA, (LONG_PTR) s);
            s->min_width = mmtb_scale_(bar, 100);
            s->max_width = mmtb_scale_(bar, 280);
            s->optimum_width = mmtb_scale_(bar, 180);
            s->options = MMTB_SHOW_ADD_TAB_BUTTON | MMTB_USE_OVERFLOW_MENU | MMTB_ALLOW_BACKGROUND_CLOSING;
            break;
          case WM_NCDESTROY:
            if (s)
              {
                int i;
                for (i = 0; i < s->count; i++) free(s->tabs[i].label);
                free(s->tabs);
                free(s);
                SetWindowLongPtrW(bar, GWLP_USERDATA, 0);
              }
            break;
          case WM_SIZE:
            mmtb_changed_(bar);
            return 0;
          case WM_SETFONT:
            s->font = (HFONT) wparam;
            if (LOWORD(lparam)) InvalidateRect(bar, NULL, FALSE);
            return 0;
          case WM_GETFONT:
            return (LRESULT) s->font;
          case WM_ERASEBKGND:
            return 1;
          case WM_PAINT:
            {
              PAINTSTRUCT ps;
              HDC dc = BeginPaint(bar, &ps);
              mmtb_paint_(bar, dc);
              EndPaint(bar, &ps);
              return 0;
            }
          case WM_PRINTCLIENT:
            mmtb_paint_(bar, (HDC) wparam);
            return 0;
          case WM_MOUSEMOVE:
            {
              BOOL on_close;
              int x = GET_X_LPARAM(lparam), hit = mmtb_hit_(bar, x, GET_Y_LPARAM(lparam), &on_close);
              if (!s->tracking)
                {
                  TRACKMOUSEEVENT track;
                  track.cbSize = sizeof track;
                  track.dwFlags = TME_LEAVE;
                  track.hwndTrack = bar;
                  track.dwHoverTime = 0;
                  s->tracking = TrackMouseEvent(&track);
                }
              if (s->drag_from >= 0 && !(s->options & MMTB_DISABLE_DRAG)
                  && (s->dragging || abs(x - s->press_x) > GetSystemMetrics(SM_CXDRAG)))
                {
                  s->dragging = TRUE;
                  if (hit >= 0 && hit != s->drag_from && mmtb_move_tab(bar, s->drag_from, hit))
                    s->drag_from = hit;
                }
              if (hit != s->hot || on_close != s->hot_close)
                {
                  s->hot = hit;
                  s->hot_close = on_close;
                  InvalidateRect(bar, NULL, FALSE);
                }
              return 0;
            }
          case WM_MOUSELEAVE:
            s->tracking = FALSE;
            s->hot = -1;
            s->hot_close = FALSE;
            InvalidateRect(bar, NULL, FALSE);
            return 0;
          case WM_LBUTTONDOWN:
            {
              BOOL on_close;
              POINT pt;
              int hit;
              pt.x = GET_X_LPARAM(lparam);
              pt.y = GET_Y_LPARAM(lparam);
              hit = mmtb_hit_(bar, pt.x, pt.y, &on_close);
              if (PtInRect(&s->add_button, pt)) { mmtb_notify_(bar, MMTBN_ADDTAB, -1, -1); return 0; }
              if (PtInRect(&s->overflow_button, pt)) { mmtb_overflow_menu_(bar); return 0; }
              if (hit < 0) return 0;
              if (on_close) { s->pressed_close = hit + 1; return 0; }
              mmtb_select(bar, hit);
              s->drag_from = hit;
              s->press_x = pt.x;
              s->dragging = FALSE;
              SetCapture(bar);
              return 0;
            }
          case WM_LBUTTONUP:
            {
              BOOL on_close;
              int hit = mmtb_hit_(bar, GET_X_LPARAM(lparam), GET_Y_LPARAM(lparam), &on_close);
              if (s->pressed_close && on_close && hit == s->pressed_close - 1) mmtb_close_tab(bar, hit);
              s->pressed_close = 0;
              if (s->drag_from >= 0)
                {
                  int from = s->drag_from;
                  ReleaseCapture();
                  s->drag_from = -1;
                  if (s->dragging) mmtb_notify_(bar, MMTBN_DIDMOVE, from, from);
                  s->dragging = FALSE;
                }
              return 0;
            }
          case WM_MBUTTONUP:
            {
              BOOL on_close;
              int hit = mmtb_hit_(bar, GET_X_LPARAM(lparam), GET_Y_LPARAM(lparam), &on_close);
              if (hit >= 0) mmtb_close_tab(bar, hit);
              return 0;
            }
          case WM_CAPTURECHANGED:
            s->drag_from = -1;
            s->dragging = FALSE;
            return 0;
          }
        return DefWindowProcW(bar, message, wparam, lparam);
      }

      static inline BOOL mmtb_register_class(void)
      {
        WNDCLASSEXW wc;
        HINSTANCE instance = GetModuleHandleW(NULL);
        if (GetClassInfoExW(instance, MMTB_CLASS, &wc)) return TRUE;
        memset(&wc, 0, sizeof wc);
        wc.cbSize = sizeof wc;
        wc.style = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS;
        wc.lpfnWndProc = mmtb_wndproc_;
        wc.hInstance = instance;
        wc.hCursor = LoadCursorW(NULL, (LPCWSTR) IDC_ARROW);
        wc.lpszClassName = MMTB_CLASS;
        return RegisterClassExW(&wc) != 0;
      }

      /* Creates a tab bar as a child of parent. id is the control id used in WM_NOTIFY. */
      static inline HWND mmtb_create(HWND parent, int x, int y, int width, int height, UINT id)
      {
        if (!mmtb_register_class()) return NULL;
        return CreateWindowExW(0, MMTB_CLASS, L"", WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, x, y, width, height,
                               parent, (HMENU) (UINT_PTR) id, GetModuleHandleW(NULL), NULL);
      }

      #ifdef __cplusplus
      }
      #endif

      #endif /* MMTABBARVIEW_H */
    C
    (lib/"pkgconfig/mmtabbarview.pc").write <<~PC
      prefix=${pcfiledir}/../..
      includedir=${prefix}/include

      Name: MMTabBarView
      Description: Safari-style tab bar control for Win32 (header-only)
      Version: #{version}
      Libs: -luser32 -lgdi32
      Cflags: -I${includedir}
    PC
    doc.install "README.md"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the control's API and the pkg-config file.
    header = (include/"MMTabBarView/MMTabBarView.h").read
    %w[mmtb_create mmtb_add_tab mmtb_close_tab mmtb_move_tab mmtb_set_options MMTBN_SHOULDCLOSE
       MMTB_USE_OVERFLOW_MENU RegisterClassExW].each do |symbol|
      assert_match symbol, header
    end
    assert_match "Libs: -luser32 -lgdi32", (lib/"pkgconfig/mmtabbarview.pc").read
  end
end
