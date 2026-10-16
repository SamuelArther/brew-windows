class Libevdev < Formula
  desc "Wrapper library for evdev devices"
  homepage "https://www.freedesktop.org/wiki/Software/libevdev/"
  url "https://www.freedesktop.org/software/libevdev/libevdev-1.14.0.tar.xz"
  sha256 "5a0966c7110648665983848bad696c7acba614a2160d2865d535397101007332"
  license "MIT"

  # libevdev wraps the kernel's evdev input devices. On Windows input devices are HID devices, so
  # upstream's libevdev.c (device model, event sync, name tables) is built unchanged on a Windows
  # evdev layer: a HID device opened as /dev/input/eventN (the Nth HID device interface) or by its
  # \\?\hid#... path answers the evdev ioctls from its HID report descriptor (axes, hats, buttons,
  # keyboard and consumer keys, digitizer usages map to ABS_*/REL_*/BTN_*/KEY_* as Linux's hid-input
  # maps them), and a reader thread turns its input reports into input_event records for read() and
  # poll(). Windows keeps keyboards and mice to itself, so opening those fails with EACCES. The
  # uinput API (libevdev_uinput_*) injects events into the session with SendInput: keys, mouse
  # buttons, relative and absolute motion and wheels. Force feedback and EVIOCGRAB have no evdev
  # path on Windows and return ENOTSUP.

  depends_on "python@3.15" => :build

  def install
    (buildpath/"win/config.h").write <<~'C'
      /* libevdev's configure results for MinGW. */
      #define _GNU_SOURCE 1
      #include "evdev-win.h"
    C
    (buildpath/"win/evdev-win.h").write <<~'C'
      /* Included first in every libevdev source on Windows: read(), write() and open() of input devices
       * go through evdev-win.c, which serves HID devices as evdev nodes. */
      #ifndef LIBEVDEV_WIN_EVDEV_WIN_H
      #define LIBEVDEV_WIN_EVDEV_WIN_H
      #include <io.h>
      #include <fcntl.h>
      #include <unistd.h>
      #include <sys/types.h>

      int evdev_win_read(int fd, void *buffer, size_t count);
      int evdev_win_write(int fd, const void *buffer, size_t count);
      int evdev_win_open(const char *path, int flags, ...);

      #define read(fd, buffer, count) evdev_win_read((fd), (buffer), (count))
      #define write(fd, buffer, count) evdev_win_write((fd), (buffer), (count))
      #define open evdev_win_open
      #ifndef O_NONBLOCK
      #define O_NONBLOCK 0x10000000
      #endif
      #ifndef O_CLOEXEC
      #define O_CLOEXEC 0
      #endif
      #endif
    C
    (buildpath/"win/poll.h").write <<~'C'
      /* <poll.h> for building libevdev: poll() over input device descriptors (see evdev-win.c). */
      #ifndef LIBEVDEV_WIN_POLL_H
      #define LIBEVDEV_WIN_POLL_H
      #define pollfd evdev_win_pollfd
      #define poll evdev_win_poll
      #define POLLIN 0x0001
      #define POLLPRI 0x0002
      #define POLLOUT 0x0004
      #define POLLERR 0x0008
      #define POLLHUP 0x0010
      #define POLLNVAL 0x0020
      struct evdev_win_pollfd { int fd; short events; short revents; };
      int evdev_win_poll(struct evdev_win_pollfd *fds, unsigned long nfds, int timeout);
      #endif
    C
    (buildpath/"win/sys/ioctl.h").write <<~'C'
      /* <sys/ioctl.h> for libevdev on Windows: Linux's ioctl request encoding (asm-generic/ioctl.h) and
       * ioctl() itself, which libevdev implements for input devices (HID devices opened as files). */
      #ifndef LIBEVDEV_WIN_SYS_IOCTL_H
      #define LIBEVDEV_WIN_SYS_IOCTL_H

      #define _IOC_NRBITS 8
      #define _IOC_TYPEBITS 8
      #define _IOC_SIZEBITS 14
      #define _IOC_DIRBITS 2
      #define _IOC_NRMASK ((1UL << _IOC_NRBITS) - 1)
      #define _IOC_TYPEMASK ((1UL << _IOC_TYPEBITS) - 1)
      #define _IOC_SIZEMASK ((1UL << _IOC_SIZEBITS) - 1)
      #define _IOC_DIRMASK ((1UL << _IOC_DIRBITS) - 1)
      #define _IOC_NRSHIFT 0
      #define _IOC_TYPESHIFT (_IOC_NRSHIFT + _IOC_NRBITS)
      #define _IOC_SIZESHIFT (_IOC_TYPESHIFT + _IOC_TYPEBITS)
      #define _IOC_DIRSHIFT (_IOC_SIZESHIFT + _IOC_SIZEBITS)
      #define _IOC_NONE 0UL
      #define _IOC_WRITE 1UL
      #define _IOC_READ 2UL

      #define _IOC(dir, type, nr, size) \
          ((unsigned long)(((unsigned long)(dir) << _IOC_DIRSHIFT) | ((unsigned long)(type) << _IOC_TYPESHIFT) | \
                           ((unsigned long)(nr) << _IOC_NRSHIFT) | ((unsigned long)(size) << _IOC_SIZESHIFT)))
      #define _IO(type, nr) _IOC(_IOC_NONE, (type), (nr), 0)
      #define _IOR(type, nr, size) _IOC(_IOC_READ, (type), (nr), sizeof(size))
      #define _IOW(type, nr, size) _IOC(_IOC_WRITE, (type), (nr), sizeof(size))
      #define _IOWR(type, nr, size) _IOC(_IOC_READ | _IOC_WRITE, (type), (nr), sizeof(size))
      #define _IOC_DIR(nr) (((nr) >> _IOC_DIRSHIFT) & _IOC_DIRMASK)
      #define _IOC_TYPE(nr) (((nr) >> _IOC_TYPESHIFT) & _IOC_TYPEMASK)
      #define _IOC_NR(nr) (((nr) >> _IOC_NRSHIFT) & _IOC_NRMASK)
      #define _IOC_SIZE(nr) (((nr) >> _IOC_SIZESHIFT) & _IOC_SIZEMASK)

      #ifdef __cplusplus
      extern "C" {
      #endif
      int ioctl(int fd, unsigned long request, ...);
      #ifdef __cplusplus
      }
      #endif

      #endif
    C
    (buildpath/"win/linux/types.h").write <<~'C'
      /* <linux/types.h>: the kernel's fixed-width type names, for <linux/input.h> on Windows. */
      #ifndef LIBEVDEV_WIN_LINUX_TYPES_H
      #define LIBEVDEV_WIN_LINUX_TYPES_H
      #include <stdint.h>

      typedef int8_t __s8;
      typedef uint8_t __u8;
      typedef int16_t __s16;
      typedef uint16_t __u16;
      typedef int32_t __s32;
      typedef uint32_t __u32;
      typedef int64_t __s64;
      typedef uint64_t __u64;
      typedef uint16_t __le16;
      typedef uint32_t __le32;
      typedef uint64_t __le64;
      typedef unsigned long __kernel_ulong_t;
      #endif
    C
    (buildpath/"win/evdev-win.c").write <<~'C'
      /* evdev on Windows: HID devices served as Linux input event devices.
       *
       * A descriptor opened on a HID device (\\?\hid#..., or /dev/input/eventN for the Nth HID device in
       * Windows' enumeration order) answers the evdev ioctls libevdev uses (EVIOCGBIT, EVIOCGABS,
       * EVIOCGNAME, EVIOCGID, EVIOCGKEY ...) from the device's HID report descriptor: Generic Desktop
       * axes become ABS_X..ABS_RZ (or REL_* for relative ones), hats ABS_HAT0X/Y, buttons BTN_*,
       * keyboard usages KEY_* (the kernel's hid-input table), consumer controls the media KEY_*s and
       * digitizer usages BTN_TOUCH/ABS_PRESSURE/ABS_TILT_*. A reader thread turns input reports into
       * struct input_event records with SYN_REPORT after each report; read() and poll() take them from
       * there. Windows opens keyboards and mice exclusively for itself, so those can't be read this way
       * (open fails with EACCES), as on Linux without permission on the event node. */
      #define WIN32_LEAN_AND_MEAN
      #include <windows.h>
      #include <hidsdi.h>
      #include <hidpi.h>
      #include <setupapi.h>
      #include <errno.h>
      #include <fcntl.h>
      #include <io.h>
      #include <stdarg.h>
      #include <stdbool.h>
      #include <stdio.h>
      #include <stdlib.h>
      #include <string.h>
      #include <time.h>
      #include <sys/ioctl.h>
      #include <linux/input.h>
      #include "poll.h"
      #include "evdev-win.h"

      #ifndef CLOCK_REALTIME
      #define CLOCK_REALTIME 0
      #endif
      #ifndef CLOCK_MONOTONIC
      #define CLOCK_MONOTONIC 1
      #endif

      #undef read
      #undef write
      #undef open

      #ifndef ENOTTY
      #define ENOTTY 25
      #endif
      #ifndef ENOTSUP
      #define ENOTSUP 129
      #endif
      #ifndef ENODEV
      #define ENODEV 19
      #endif

      #define BITS_BYTES(n) (((n) + 7) / 8)
      #define QUEUE_MAX 4096

      enum { KIND_OTHER, KIND_MOUSE, KIND_JOYSTICK, KIND_GAMEPAD, KIND_KEYBOARD, KIND_PEN, KIND_TOUCHSCREEN, KIND_TOUCHPAD, KIND_CONSUMER };

      typedef struct { USAGE page, usage; int code; bool down; } button_map;
      typedef struct { USAGE page, usage; USHORT link; int type, code, hat; LONG min, max; bool is_signed; LONG last; } value_map;

      typedef struct device {
          int fd;
          HANDLE proxy, handle;   /* the descriptor's null-device handle; the HID device */
          PHIDP_PREPARSED_DATA ppd;
          HIDP_CAPS caps;
          int kind;
          char name[256], phys[512], uniq[256];
          struct input_id id;
          uint8_t evbits[BITS_BYTES(EV_CNT)], keybits[BITS_BYTES(KEY_CNT)], relbits[BITS_BYTES(REL_CNT)];
          uint8_t absbits[BITS_BYTES(ABS_CNT)], props[BITS_BYTES(INPUT_PROP_CNT)], keystate[BITS_BYTES(KEY_CNT)];
          struct input_absinfo abs[ABS_CNT];
          button_map *buttons;
          int nbuttons;
          value_map *values;
          int nvalues;
          USAGE_AND_PAGE *usage_list;
          ULONG usage_list_length;
          /* event queue, filled by the reader thread */
          CRITICAL_SECTION lock;
          CONDITION_VARIABLE changed;
          HANDLE readable;   /* manual-reset: signaled while events are queued or the device failed */
          struct input_event *queue;
          int head, count;
          HANDLE thread;
          bool failed, nonblocking;
          int clock;
          struct device *next;
      } device;

      static SRWLOCK devices_lock = SRWLOCK_INIT;
      static device *devices;

      static void set_bit8(uint8_t *bits, int bit) { bits[bit / 8] |= (uint8_t)(1u << (bit % 8)); }
      static bool test_bit8(const uint8_t *bits, int bit) { return (bits[bit / 8] >> (bit % 8)) & 1; }

      /* The kernel's HID keyboard usage -> KEY_* table (drivers/hid/hid-input.c); 0 = unmapped. */
      static const unsigned char hid_keyboard[256] = {
            0,  0,  0,  0, 30, 48, 46, 32, 18, 33, 34, 35, 23, 36, 37, 38,
           50, 49, 24, 25, 16, 19, 31, 20, 22, 47, 17, 45, 21, 44,  2,  3,
            4,  5,  6,  7,  8,  9, 10, 11, 28,  1, 14, 15, 57, 12, 13, 26,
           27, 43, 43, 39, 40, 41, 51, 52, 53, 58, 59, 60, 61, 62, 63, 64,
           65, 66, 67, 68, 87, 88, 99, 70,119,110,102,104,111,107,109,106,
          105,108,103, 69, 98, 55, 74, 78, 96, 79, 80, 81, 75, 76, 77, 71,
           72, 73, 82, 83, 86,127,116,117,183,184,185,186,187,188,189,190,
          191,192,193,194,134,138,130,132,128,129,131,137,133,135,136,113,
          115,114,  0,  0,  0,121,  0, 89, 93,124, 92, 94, 95,  0,  0,  0,
          122,123, 90, 91, 85,  0,  0,  0,  0,  0,  0,  0,111,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,179,180,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,111,  0,  0,  0,  0,  0,  0,  0,
           29, 42, 56,125, 97, 54,100,126,164,166,165,163,161,115,114,113,
          150,158,159,128,136,177,178,176,142,152,173,140,  0,  0,  0,  0,
      };

      static const struct { USAGE usage; int code; } consumer_keys[] = {
          { 0x30, KEY_POWER }, { 0x40, KEY_MENU }, { 0x6f, KEY_BRIGHTNESSUP }, { 0x70, KEY_BRIGHTNESSDOWN },
          { 0xb0, KEY_PLAYCD }, { 0xb1, KEY_PAUSECD }, { 0xb3, KEY_FASTFORWARD }, { 0xb4, KEY_REWIND },
          { 0xb5, KEY_NEXTSONG }, { 0xb6, KEY_PREVIOUSSONG }, { 0xb7, KEY_STOPCD }, { 0xb8, KEY_EJECTCD },
          { 0xcd, KEY_PLAYPAUSE }, { 0xe2, KEY_MUTE }, { 0xe9, KEY_VOLUMEUP }, { 0xea, KEY_VOLUMEDOWN },
          { 0x18a, KEY_MAIL }, { 0x192, KEY_CALC }, { 0x194, KEY_FILE }, { 0x221, KEY_SEARCH },
          { 0x223, KEY_HOMEPAGE }, { 0x224, KEY_BACK }, { 0x225, KEY_FORWARD }, { 0x227, KEY_REFRESH },
          { 0x22a, KEY_BOOKMARKS },
      };

      static const int gamepad_buttons[] = {
          BTN_SOUTH, BTN_EAST, BTN_C, BTN_NORTH, BTN_WEST, BTN_Z, BTN_TL, BTN_TR,
          BTN_TL2, BTN_TR2, BTN_SELECT, BTN_START, BTN_MODE, BTN_THUMBL, BTN_THUMBR,
      };

      static int button_code(const device *d, USAGE page, USAGE usage)
      {
          size_t i;
          switch (page) {
          case HID_USAGE_PAGE_BUTTON: {
              int n = (int)usage - 1;
              if (n < 0) return -1;
              if (d->kind == KIND_MOUSE || d->kind == KIND_TOUCHPAD) return n < 8 ? BTN_LEFT + n : BTN_MISC + (n - 8 < 10 ? n - 8 : 9);
              if (d->kind == KIND_GAMEPAD && n < (int)(sizeof(gamepad_buttons) / sizeof(gamepad_buttons[0]))) return gamepad_buttons[n];
              if (d->kind == KIND_JOYSTICK || d->kind == KIND_GAMEPAD) return n < 16 ? BTN_TRIGGER + n : (n - 16 < 40 ? BTN_TRIGGER_HAPPY1 + n - 16 : -1);
              if (d->kind == KIND_PEN && n < 2) return n == 0 ? BTN_STYLUS : BTN_STYLUS2;
              return n < 10 ? BTN_0 + n : (n - 10 < 40 ? BTN_TRIGGER_HAPPY1 + n - 10 : -1);
          }
          case HID_USAGE_PAGE_KEYBOARD:
              return usage < 256 && hid_keyboard[usage] ? hid_keyboard[usage] : -1;
          case HID_USAGE_PAGE_CONSUMER:
              for (i = 0; i < sizeof(consumer_keys) / sizeof(consumer_keys[0]); i++)
                  if (consumer_keys[i].usage == usage) return consumer_keys[i].code;
              return -1;
          case HID_USAGE_PAGE_DIGITIZER:
              switch (usage) {
              case 0x42: return BTN_TOUCH;
              case 0x44: return BTN_STYLUS;
              case 0x5a: return BTN_STYLUS2;
              case 0x45: case 0x3c: return BTN_TOOL_RUBBER;
              case 0x32: return d->kind == KIND_PEN ? BTN_TOOL_PEN : BTN_TOOL_FINGER;
              }
              return -1;
          case HID_USAGE_PAGE_GENERIC:
              switch (usage) {
              case 0x81: return KEY_POWER;
              case 0x82: return KEY_SLEEP;
              case 0x83: return KEY_WAKEUP;
              case 0x90: return BTN_DPAD_UP;
              case 0x91: return BTN_DPAD_DOWN;
              case 0x92: return BTN_DPAD_RIGHT;
              case 0x93: return BTN_DPAD_LEFT;
              }
              return -1;
          }
          return -1;
      }

      /* EV_ABS / EV_REL code for a value usage; *type gets the event type. */
      static int value_code(const device *d, USAGE page, USAGE usage, bool absolute, int *type)
      {
          *type = absolute ? EV_ABS : EV_REL;
          switch (page) {
          case HID_USAGE_PAGE_GENERIC:
              if (usage >= 0x30 && usage <= 0x35) return absolute ? ABS_X + (usage - 0x30) : REL_X + (usage - 0x30);
              if (usage == 0x36) { *type = EV_ABS; return ABS_THROTTLE; }
              if (usage == 0x37) return absolute ? ABS_RUDDER : REL_DIAL;
              if (usage == 0x38) return absolute ? ABS_WHEEL : REL_WHEEL;
              if (usage == 0x39) { *type = EV_ABS; return ABS_HAT0X; }
              return -1;
          case HID_USAGE_PAGE_SIMULATION:
              *type = EV_ABS;
              switch (usage) {
              case 0xba: return ABS_RUDDER;
              case 0xbb: return ABS_THROTTLE;
              case 0xc4: return ABS_GAS;
              case 0xc5: return ABS_BRAKE;
              case 0xc8: return ABS_WHEEL;
              }
              return -1;
          case HID_USAGE_PAGE_CONSUMER:
              if (usage == 0x238) { *type = EV_REL; return REL_HWHEEL; }
              if (usage == 0xe0) { *type = EV_ABS; return ABS_VOLUME; }
              return -1;
          case HID_USAGE_PAGE_DIGITIZER:
              *type = EV_ABS;
              switch (usage) {
              case 0x30: return ABS_PRESSURE;
              case 0x3d: return ABS_TILT_X;
              case 0x3e: return ABS_TILT_Y;
              case 0x48: return ABS_TOOL_WIDTH;
              }
              return -1;
          }
          (void)d;
          return -1;
      }

      static void utf16_to_utf8(const wchar_t *w, char *out, size_t size)
      {
          if (WideCharToMultiByte(CP_UTF8, 0, w, -1, out, (int)size, NULL, NULL) <= 0) out[0] = 0;
      }

      static bool add_value(device *d, USAGE page, USAGE usage, USHORT link, const HIDP_VALUE_CAPS *vc)
      {
          int type, code = value_code(d, page, usage, vc->IsAbsolute, &type);
          value_map *v;
          LONG min = vc->LogicalMin, max = vc->LogicalMax;
          if (code < 0) return false;
          if (code == ABS_HAT0X) {
              /* one hat switch = two axes; further hats take the next pairs */
              while (code <= ABS_HAT3X && test_bit8(d->absbits, code)) code += 2;
              if (code > ABS_HAT3X) return false;
          }
          else if (type == EV_ABS ? test_bit8(d->absbits, code) : test_bit8(d->relbits, code)) return false;
          if (max < min && vc->BitSize < 32) max = (LONG)((1UL << vc->BitSize) - 1);   /* unsigned field with a negative-looking max */
          v = &d->values[d->nvalues++];
          memset(v, 0, sizeof(*v));
          v->page = page; v->usage = usage; v->link = link; v->type = type; v->code = code;
          v->min = min; v->max = max; v->is_signed = min < 0;
          v->hat = code >= ABS_HAT0X && code <= ABS_HAT3Y && page == HID_USAGE_PAGE_GENERIC && usage == 0x39;
          v->last = LONG_MIN;
          set_bit8(d->evbits, type);
          if (type == EV_REL) { set_bit8(d->relbits, code); return true; }
          if (v->hat) {
              set_bit8(d->absbits, code);
              set_bit8(d->absbits, code + 1);
              d->abs[code].minimum = d->abs[code + 1].minimum = -1;
              d->abs[code].maximum = d->abs[code + 1].maximum = 1;
              return true;
          }
          set_bit8(d->absbits, code);
          d->abs[code].minimum = min;
          d->abs[code].maximum = max;
          if ((d->kind == KIND_JOYSTICK || d->kind == KIND_GAMEPAD) && code <= ABS_RZ) {
              d->abs[code].fuzz = (max - min) >> 8;
              d->abs[code].flat = (max - min) >> 4;
          }
          d->abs[code].value = min + (max - min) / 2;
          if (code == ABS_X || code == ABS_Y) {
              /* resolution in units per mm, when the descriptor gives a physical size in cm or inches */
              LONG range = vc->PhysicalMax - vc->PhysicalMin;
              ULONG unit = vc->Units & 0xf;
              if (range > 0 && (unit == 1 || unit == 3)) {
                  double mm = range * (unit == 1 ? 10.0 : 25.4);
                  int exponent = (int)((vc->UnitsExp & 0x8) ? (LONG)vc->UnitsExp - 16 : (LONG)vc->UnitsExp);
                  for (; exponent < 0; exponent++) mm /= 10;
                  for (; exponent > 0; exponent--) mm *= 10;
                  if (mm > 0) d->abs[code].resolution = (int)((max - min) / mm + 0.5);
              }
          }
          return true;
      }

      static bool add_button(device *d, USAGE page, USAGE usage)
      {
          int code = button_code(d, page, usage);
          if (code < 0 || code >= KEY_CNT || test_bit8(d->keybits, code)) return false;
          d->buttons[d->nbuttons].page = page;
          d->buttons[d->nbuttons].usage = usage;
          d->buttons[d->nbuttons].code = code;
          d->buttons[d->nbuttons].down = false;
          d->nbuttons++;
          set_bit8(d->evbits, EV_KEY);
          set_bit8(d->keybits, code);
          return true;
      }

      static bool describe(device *d)
      {
          HIDD_ATTRIBUTES attributes = { sizeof(attributes) };
          HIDP_BUTTON_CAPS *bcaps = NULL;
          HIDP_VALUE_CAPS *vcaps = NULL;
          USHORT nb, nv, i;
          wchar_t text[256], manufacturer[128];
          if (!HidD_GetAttributes(d->handle, &attributes) || !HidD_GetPreparsedData(d->handle, &d->ppd)) return false;
          if (HidP_GetCaps(d->ppd, &d->caps) != HIDP_STATUS_SUCCESS) return false;
          d->id.bustype = BUS_USB;
          d->id.vendor = attributes.VendorID;
          d->id.product = attributes.ProductID;
          d->id.version = attributes.VersionNumber;
          if (strstr(d->phys, "{00001124-") || strstr(d->phys, "{00001812-") || _strnicmp(d->phys, "bth", 3) == 0) d->id.bustype = BUS_BLUETOOTH;

          switch (d->caps.UsagePage) {
          case HID_USAGE_PAGE_GENERIC:
              d->kind = d->caps.Usage == HID_USAGE_GENERIC_MOUSE || d->caps.Usage == HID_USAGE_GENERIC_POINTER ? KIND_MOUSE
                      : d->caps.Usage == HID_USAGE_GENERIC_JOYSTICK || d->caps.Usage == 0x08 ? KIND_JOYSTICK
                      : d->caps.Usage == HID_USAGE_GENERIC_GAMEPAD ? KIND_GAMEPAD
                      : d->caps.Usage == HID_USAGE_GENERIC_KEYBOARD || d->caps.Usage == HID_USAGE_GENERIC_KEYPAD ? KIND_KEYBOARD : KIND_OTHER;
              break;
          case HID_USAGE_PAGE_DIGITIZER:
              d->kind = d->caps.Usage == 0x02 ? KIND_PEN : d->caps.Usage == 0x04 ? KIND_TOUCHSCREEN : d->caps.Usage == 0x05 ? KIND_TOUCHPAD : KIND_OTHER;
              break;
          case HID_USAGE_PAGE_CONSUMER: d->kind = KIND_CONSUMER; break;
          default: d->kind = KIND_OTHER; break;
          }
          if (d->kind == KIND_PEN || d->kind == KIND_TOUCHSCREEN) set_bit8(d->props, INPUT_PROP_DIRECT);
          if (d->kind == KIND_TOUCHPAD) set_bit8(d->props, INPUT_PROP_POINTER);

          manufacturer[0] = text[0] = 0;
          HidD_GetManufacturerString(d->handle, manufacturer, sizeof(manufacturer));
          HidD_GetProductString(d->handle, text, sizeof(text));
          if (manufacturer[0] && text[0] && wcsncmp(text, manufacturer, wcslen(manufacturer)) != 0) {
              wchar_t joined[384];
              _snwprintf(joined, 383, L"%ls %ls", manufacturer, text);
              joined[383] = 0;
              utf16_to_utf8(joined, d->name, sizeof(d->name));
          }
          else utf16_to_utf8(text[0] ? text : manufacturer, d->name, sizeof(d->name));
          if (!d->name[0]) snprintf(d->name, sizeof(d->name), "HID %04x:%04x", d->id.vendor, d->id.product);
          text[0] = 0;
          if (HidD_GetSerialNumberString(d->handle, text, sizeof(text))) utf16_to_utf8(text, d->uniq, sizeof(d->uniq));

          set_bit8(d->evbits, EV_SYN);
          nb = d->caps.NumberInputButtonCaps;
          nv = d->caps.NumberInputValueCaps;
          bcaps = calloc(nb ? nb : 1, sizeof(*bcaps));
          vcaps = calloc(nv ? nv : 1, sizeof(*vcaps));
          if (nb) HidP_GetButtonCaps(HidP_Input, bcaps, &nb, d->ppd);
          if (nv) HidP_GetValueCaps(HidP_Input, vcaps, &nv, d->ppd);
          {
              size_t button_slots = 0, value_slots = 0;
              for (i = 0; i < nb; i++) button_slots += bcaps[i].IsRange ? (size_t)(bcaps[i].Range.UsageMax - bcaps[i].Range.UsageMin + 1) : 1;
              for (i = 0; i < nv; i++) value_slots += vcaps[i].IsRange ? (size_t)(vcaps[i].Range.UsageMax - vcaps[i].Range.UsageMin + 1) : 1;
              d->buttons = calloc(button_slots + 1, sizeof(*d->buttons));
              d->values = calloc(value_slots + 1, sizeof(*d->values));
          }
          for (i = 0; i < nv; i++) {
              USAGE u, lo = vcaps[i].IsRange ? vcaps[i].Range.UsageMin : vcaps[i].NotRange.Usage;
              USAGE hi = vcaps[i].IsRange ? vcaps[i].Range.UsageMax : lo;
              for (u = lo; u <= hi && u >= lo; u++) add_value(d, vcaps[i].UsagePage, u, vcaps[i].LinkCollection, &vcaps[i]);
          }
          for (i = 0; i < nb; i++) {
              USAGE u, lo = bcaps[i].IsRange ? bcaps[i].Range.UsageMin : bcaps[i].NotRange.Usage;
              USAGE hi = bcaps[i].IsRange ? bcaps[i].Range.UsageMax : lo;
              for (u = lo; u <= hi && u >= lo; u++) add_button(d, bcaps[i].UsagePage, u);
          }
          free(bcaps);
          free(vcaps);
          d->usage_list_length = HidP_MaxUsageListLength(HidP_Input, 0, d->ppd);
          d->usage_list = calloc(d->usage_list_length + 1, sizeof(*d->usage_list));
          return true;
      }

      static void stamp(device *d, struct input_event *ev)
      {
          if (d->clock == CLOCK_REALTIME) {
              FILETIME ft;
              ULARGE_INTEGER t;
              GetSystemTimePreciseAsFileTime(&ft);
              t.LowPart = ft.dwLowDateTime;
              t.HighPart = ft.dwHighDateTime;
              t.QuadPart -= 116444736000000000ULL;   /* 1601 -> 1970 */
              ev->input_event_sec = (long)(t.QuadPart / 10000000ULL);
              ev->input_event_usec = (long)((t.QuadPart % 10000000ULL) / 10);
          }
          else {
              LARGE_INTEGER now, frequency;
              QueryPerformanceCounter(&now);
              QueryPerformanceFrequency(&frequency);
              ev->input_event_sec = (long)(now.QuadPart / frequency.QuadPart);
              ev->input_event_usec = (long)((now.QuadPart % frequency.QuadPart) * 1000000 / frequency.QuadPart);
          }
      }

      /* Called with d->lock held. */
      static void push(device *d, int type, int code, int value)
      {
          struct input_event *ev;
          if (d->count == QUEUE_MAX) {
              /* Overflow: drop what's queued and tell the reader to resync, like the kernel's SYN_DROPPED. */
              d->count = 0;
              ev = &d->queue[d->head];
              memset(ev, 0, sizeof(*ev));
              stamp(d, ev);
              ev->type = EV_SYN;
              ev->code = SYN_DROPPED;
              d->count = 1;
          }
          ev = &d->queue[(d->head + d->count) % QUEUE_MAX];
          memset(ev, 0, sizeof(*ev));
          stamp(d, ev);
          ev->type = (unsigned short)type;
          ev->code = (unsigned short)code;
          ev->value = value;
          d->count++;
      }

      static DWORD WINAPI reader(void *arg)
      {
          device *d = arg;
          BYTE *report = malloc(d->caps.InputReportByteLength + 1);
          static const int hat_x[8] = { 0, 1, 1, 1, 0, -1, -1, -1 }, hat_y[8] = { -1, -1, 0, 1, 1, 1, 0, -1 };
          for (;;) {
              DWORD got = 0;
              int emitted = 0, i;
              ULONG n;
              if (report == NULL || !ReadFile(d->handle, report, d->caps.InputReportByteLength, &got, NULL) || got == 0) break;
              EnterCriticalSection(&d->lock);
              n = d->usage_list_length;
              if (HidP_GetUsagesEx(HidP_Input, 0, d->usage_list, &n, d->ppd, (PCHAR)report, got) == HIDP_STATUS_SUCCESS || n == 0) {
                  for (i = 0; i < d->nbuttons; i++) {
                      button_map *b = &d->buttons[i];
                      bool down = false;
                      ULONG k;
                      for (k = 0; k < n; k++)
                          if (d->usage_list[k].Usage == b->usage && d->usage_list[k].UsagePage == b->page) { down = true; break; }
                      if (down != b->down) {
                          b->down = down;
                          if (down) set_bit8(d->keystate, b->code);
                          else d->keystate[b->code / 8] &= (uint8_t)~(1u << (b->code % 8));
                          push(d, EV_KEY, b->code, down);
                          emitted++;
                      }
                  }
              }
              for (i = 0; i < d->nvalues; i++) {
                  value_map *v = &d->values[i];
                  LONG value;
                  ULONG raw;
                  NTSTATUS status;
                  if (v->is_signed) status = HidP_GetScaledUsageValue(HidP_Input, v->page, v->link, v->usage, &value, d->ppd, (PCHAR)report, got);
                  else {
                      status = HidP_GetUsageValue(HidP_Input, v->page, v->link, v->usage, &raw, d->ppd, (PCHAR)report, got);
                      value = (LONG)raw;
                  }
                  if (status != HIDP_STATUS_SUCCESS) continue;   /* not in this report */
                  if (v->type == EV_REL) {
                      if (value != 0) { push(d, EV_REL, v->code, value); emitted++; }
                      continue;
                  }
                  if (value == v->last) continue;
                  v->last = value;
                  if (v->hat) {
                      int index = (int)(value - v->min), x = 0, y = 0;
                      LONG positions = v->max - v->min + 1;
                      if (index >= 0 && index < positions) {
                          if (positions == 4) index *= 2;
                          if (index < 8) { x = hat_x[index]; y = hat_y[index]; }
                      }
                      if (d->abs[v->code].value != x) { d->abs[v->code].value = x; push(d, EV_ABS, v->code, x); emitted++; }
                      if (d->abs[v->code + 1].value != y) { d->abs[v->code + 1].value = y; push(d, EV_ABS, v->code + 1, y); emitted++; }
                      continue;
                  }
                  d->abs[v->code].value = value;
                  push(d, EV_ABS, v->code, value);
                  emitted++;
              }
              if (emitted) {
                  push(d, EV_SYN, SYN_REPORT, 0);
                  SetEvent(d->readable);
                  WakeAllConditionVariable(&d->changed);
              }
              LeaveCriticalSection(&d->lock);
          }
          free(report);
          EnterCriticalSection(&d->lock);
          d->failed = true;
          SetEvent(d->readable);
          WakeAllConditionVariable(&d->changed);
          LeaveCriticalSection(&d->lock);
          return 0;
      }

      static void destroy(device *d)
      {
          if (d->thread) {
              CancelSynchronousIo(d->thread);
              WaitForSingleObject(d->thread, 2000);
              CloseHandle(d->thread);
          }
          if (d->ppd) HidD_FreePreparsedData(d->ppd);
          if (d->handle) CloseHandle(d->handle);
          if (d->readable) CloseHandle(d->readable);
          DeleteCriticalSection(&d->lock);
          free(d->buttons);
          free(d->values);
          free(d->usage_list);
          free(d->queue);
          free(d);
      }

      /* The device behind fd; NULL for descriptors that aren't input devices. The C runtime can't hold a
       * HID handle (its file type is unknown to it), so an evdev descriptor is a C runtime descriptor on
       * the null device, and the HID handle lives here, keyed by that descriptor and its handle. */
      static device *lookup(int fd)
      {
          HANDLE proxy = fd >= 0 ? (HANDLE)_get_osfhandle(fd) : INVALID_HANDLE_VALUE;
          device *d;
          if (proxy == INVALID_HANDLE_VALUE) return NULL;
          AcquireSRWLockShared(&devices_lock);
          for (d = devices; d != NULL; d = d->next) if (d->fd == fd && d->proxy == proxy) break;
          ReleaseSRWLockShared(&devices_lock);
          return d;
      }

      /* Forget devices whose descriptor number was closed and reused (or is being reused now). */
      static void forget(int fd)
      {
          device **link, *old;
          AcquireSRWLockExclusive(&devices_lock);
          for (link = &devices; *link != NULL;) {
              if ((*link)->fd == fd) { old = *link; *link = old->next; destroy(old); }
              else link = &(*link)->next;
          }
          ReleaseSRWLockExclusive(&devices_lock);
      }

      static device *attach(int fd, HANDLE proxy, HANDLE handle, const char *path)
      {
          device *d = calloc(1, sizeof(*d));
          if (d == NULL) return NULL;
          d->fd = fd;
          d->proxy = proxy;
          d->handle = handle;
          d->clock = CLOCK_REALTIME;
          InitializeCriticalSection(&d->lock);
          InitializeConditionVariable(&d->changed);
          d->readable = CreateEventW(NULL, TRUE, FALSE, NULL);
          d->queue = calloc(QUEUE_MAX, sizeof(*d->queue));
          snprintf(d->phys, sizeof(d->phys), "%s", path);
          if (!describe(d) || d->queue == NULL) { destroy(d); return NULL; }
          d->thread = CreateThread(NULL, 0, reader, d, 0, NULL);
          forget(fd);
          AcquireSRWLockExclusive(&devices_lock);
          d->next = devices;
          devices = d;
          ReleaseSRWLockExclusive(&devices_lock);
          return d;
      }

      static int copy_out(void *arg, unsigned long size, const void *data, size_t length)
      {
          size_t n = size < length ? size : length;
          if (arg == NULL) { errno = EFAULT; return -1; }
          memset(arg, 0, size);
          memcpy(arg, data, n);
          return (int)n;
      }

      int ioctl(int fd, unsigned long request, ...)
      {
          va_list va;
          void *arg;
          device *d;
          unsigned long nr = _IOC_NR(request), size = _IOC_SIZE(request);
          va_start(va, request);
          arg = va_arg(va, void *);
          va_end(va);
          d = lookup(fd);
          if (d == NULL || _IOC_TYPE(request) != 'E') { errno = ENOTTY; return -1; }
          if (_IOC_DIR(request) == _IOC_READ) {
              if (nr == 0x01) { int version = EV_VERSION; return copy_out(arg, sizeof(int), &version, sizeof(version)) < 0 ? -1 : 0; }
              if (nr == 0x02) return copy_out(arg, sizeof(d->id), &d->id, sizeof(d->id)) < 0 ? -1 : 0;
              if (nr == 0x06) return copy_out(arg, size, d->name, strlen(d->name) + 1);
              if (nr == 0x07) return copy_out(arg, size, d->phys, strlen(d->phys) + 1);
              if (nr == 0x08) {
                  if (!d->uniq[0]) { errno = ENOENT; return -1; }
                  return copy_out(arg, size, d->uniq, strlen(d->uniq) + 1);
              }
              if (nr == 0x09) return copy_out(arg, size, d->props, sizeof(d->props));
              if (nr == 0x18) {
                  int r;
                  EnterCriticalSection(&d->lock);
                  r = copy_out(arg, size, d->keystate, sizeof(d->keystate));
                  LeaveCriticalSection(&d->lock);
                  return r;
              }
              if (nr == 0x19 || nr == 0x1a || nr == 0x1b) { static const uint8_t none[BITS_BYTES(KEY_CNT)]; return copy_out(arg, size, none, size < sizeof(none) ? size : sizeof(none)); }
              if (nr >= 0x20 && nr < 0x20 + EV_CNT) {
                  static const uint8_t none[BITS_BYTES(KEY_CNT)];
                  switch (nr - 0x20) {
                  case 0: return copy_out(arg, size, d->evbits, sizeof(d->evbits));
                  case EV_KEY: return copy_out(arg, size, d->keybits, sizeof(d->keybits));
                  case EV_REL: return copy_out(arg, size, d->relbits, sizeof(d->relbits));
                  case EV_ABS: return copy_out(arg, size, d->absbits, sizeof(d->absbits));
                  default: return copy_out(arg, size, none, size < sizeof(none) ? size : sizeof(none));
                  }
              }
              if (nr >= 0x40 && nr < 0x40 + ABS_CNT) {
                  int r;
                  EnterCriticalSection(&d->lock);
                  r = copy_out(arg, sizeof(struct input_absinfo), &d->abs[nr - 0x40], sizeof(struct input_absinfo));
                  LeaveCriticalSection(&d->lock);
                  return r < 0 ? -1 : 0;
              }
              if (nr == 0x84) { int effects = 0; return copy_out(arg, sizeof(int), &effects, sizeof(effects)) < 0 ? -1 : 0; }
              errno = EINVAL;   /* EVIOCGREP, EVIOCGKEYCODE, EVIOCGMTSLOTS, EVIOCGMASK: nothing to report */
              return -1;
          }
          if (_IOC_DIR(request) == _IOC_WRITE) {
              if (nr >= 0xc0 && nr < 0xc0 + ABS_CNT && arg != NULL) {
                  EnterCriticalSection(&d->lock);
                  d->abs[nr - 0xc0] = *(const struct input_absinfo *)arg;
                  LeaveCriticalSection(&d->lock);
                  return 0;
              }
              if (nr == 0xa0 && arg != NULL) {
                  int clock = *(const int *)arg;
                  if (clock != CLOCK_REALTIME && clock != CLOCK_MONOTONIC) { errno = EINVAL; return -1; }
                  d->clock = clock;
                  return 0;
              }
              /* EVIOCGRAB: Windows offers no exclusive access to a HID device it already shares.
               * EVIOCSFF/EVIOCRMFF: force feedback goes through the device's HID PID or XInput, not evdev. */
              errno = ENOTSUP;
              return -1;
          }
          errno = EINVAL;
          return -1;
      }

      int evdev_win_read(int fd, void *buffer, size_t count)
      {
          device *d = lookup(fd);
          struct input_event *out = buffer;
          size_t want = count / sizeof(struct input_event), n = 0;
          if (d == NULL) return _read(fd, buffer, (unsigned int)count);
          if (want == 0) { errno = EINVAL; return -1; }
          EnterCriticalSection(&d->lock);
          while (d->count == 0 && !d->failed && !d->nonblocking) SleepConditionVariableCS(&d->changed, &d->lock, INFINITE);
          while (n < want && d->count > 0) {
              out[n++] = d->queue[d->head];
              d->head = (d->head + 1) % QUEUE_MAX;
              d->count--;
          }
          if (d->count == 0 && !d->failed) ResetEvent(d->readable);
          LeaveCriticalSection(&d->lock);
          if (n == 0) { errno = d->failed ? ENODEV : EAGAIN; return -1; }
          return (int)(n * sizeof(struct input_event));
      }

      int evdev_win_write(int fd, const void *buffer, size_t count)
      {
          if (lookup(fd) != NULL) { errno = ENOTSUP; return -1; }   /* LED output reports aren't sent */
          return _write(fd, buffer, (unsigned int)count);
      }

      int evdev_win_poll(struct evdev_win_pollfd *fds, unsigned long nfds, int timeout)
      {
          HANDLE waits[MAXIMUM_WAIT_OBJECTS];
          DWORD start = GetTickCount();
          for (;;) {
              int ready = 0, nwaits = 0;
              unsigned long i;
              for (i = 0; i < nfds; i++) {
                  device *d = lookup(fds[i].fd);
                  fds[i].revents = 0;
                  if (fds[i].fd < 0) continue;
                  if (d == NULL) {
                      if ((HANDLE)_get_osfhandle(fds[i].fd) == INVALID_HANDLE_VALUE) fds[i].revents = POLLNVAL;
                      else fds[i].revents = fds[i].events & (POLLIN | POLLOUT);   /* files and pipes: always ready */
                  }
                  else {
                      EnterCriticalSection(&d->lock);
                      if (d->count > 0) fds[i].revents |= fds[i].events & POLLIN;
                      if (d->failed) fds[i].revents |= POLLHUP | POLLERR;
                      LeaveCriticalSection(&d->lock);
                      if (nwaits < MAXIMUM_WAIT_OBJECTS) waits[nwaits++] = d->readable;
                  }
                  if (fds[i].revents) ready++;
              }
              if (ready || timeout == 0) return ready;
              {
                  DWORD elapsed = GetTickCount() - start, wait;
                  if (timeout > 0 && elapsed >= (DWORD)timeout) return 0;
                  wait = timeout < 0 ? INFINITE : (DWORD)timeout - elapsed;
                  if (nwaits == 0) { Sleep(wait == INFINITE ? 100 : wait); if (timeout >= 0) return 0; continue; }
                  if (WaitForMultipleObjects((DWORD)nwaits, waits, FALSE, wait) == WAIT_TIMEOUT) return 0;
              }
          }
      }

      /* /dev/input/eventN: the Nth HID device interface, in SetupAPI's enumeration order. */
      static wchar_t *event_node_path(int index)
      {
          GUID hid;
          HDEVINFO set;
          SP_DEVICE_INTERFACE_DATA iface = { sizeof(iface) };
          wchar_t *path = NULL;
          HidD_GetHidGuid(&hid);
          set = SetupDiGetClassDevsW(&hid, NULL, NULL, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
          if (set == INVALID_HANDLE_VALUE) return NULL;
          if (SetupDiEnumDeviceInterfaces(set, NULL, &hid, (DWORD)index, &iface)) {
              DWORD size = 0;
              SP_DEVICE_INTERFACE_DETAIL_DATA_W *detail;
              SetupDiGetDeviceInterfaceDetailW(set, &iface, NULL, 0, &size, NULL);
              detail = malloc(size);
              if (detail != NULL) {
                  detail->cbSize = sizeof(*detail);
                  if (SetupDiGetDeviceInterfaceDetailW(set, &iface, detail, size, NULL, NULL)) path = _wcsdup(detail->DevicePath);
                  free(detail);
              }
          }
          SetupDiDestroyDeviceInfoList(set);
          return path;
      }

      int evdev_win_open(const char *path, int flags, ...)
      {
          wchar_t *wide = NULL;
          HANDLE handle, proxy;
          DWORD access = 0;
          device *d;
          int fd, mode = 0;
          if (flags & O_CREAT) {
              va_list va;
              va_start(va, flags);
              mode = va_arg(va, int);
              va_end(va);
          }
          if (path != NULL && strncmp(path, "/dev/input/event", 16) == 0) wide = event_node_path(atoi(path + 16));
          else if (path != NULL && (_strnicmp(path, "\\\\?\\hid#", 8) == 0 || _strnicmp(path, "\\\\.\\hid#", 8) == 0)) {
              int n = MultiByteToWideChar(CP_UTF8, 0, path, -1, NULL, 0);
              wide = malloc(sizeof(wchar_t) * (size_t)n);
              if (wide != NULL) MultiByteToWideChar(CP_UTF8, 0, path, -1, wide, n);
          }
          else return _open(path, flags & ~(O_NONBLOCK | O_CLOEXEC), mode);
          if (wide == NULL) { errno = ENOENT; return -1; }
          switch (flags & 3) {
          case O_WRONLY: access = GENERIC_WRITE; break;
          case O_RDWR: access = GENERIC_READ | GENERIC_WRITE; break;
          default: access = GENERIC_READ; break;
          }
          handle = CreateFileW(wide, access, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
          free(wide);
          if (handle == INVALID_HANDLE_VALUE) {
              DWORD error = GetLastError();
              errno = error == ERROR_ACCESS_DENIED || error == ERROR_SHARING_VIOLATION ? EACCES : ENOENT;
              return -1;
          }
          proxy = CreateFileW(L"NUL", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
          fd = proxy == INVALID_HANDLE_VALUE ? -1 : _open_osfhandle((intptr_t)proxy, (flags & 3) | _O_BINARY);
          if (fd < 0) {
              if (proxy != INVALID_HANDLE_VALUE) CloseHandle(proxy);
              CloseHandle(handle);
              errno = EMFILE;
              return -1;
          }
          d = attach(fd, proxy, handle, path);
          if (d == NULL) {
              _close(fd);
              errno = ENODEV;
              return -1;
          }
          if (flags & O_NONBLOCK) d->nonblocking = true;
          return fd;
      }
    C
    (buildpath/"win/libevdev-uinput.c").write <<~'C'
      /* libevdev's uinput API on Windows: a "virtual device" injects its events into the session's input
       * stream with SendInput, the way a uinput device feeds the kernel's input core. Events are batched
       * until SYN_REPORT, like the kernel. EV_KEY keys become keyboard scan codes (evdev key codes are
       * PC set-1 scan codes for the main block; the rest use the extended or virtual-key equivalents),
       * BTN_LEFT/RIGHT/MIDDLE/SIDE/EXTRA (and BTN_TOUCH on absolute devices) mouse buttons, REL_X/REL_Y
       * relative motion, REL_WHEEL/REL_HWHEEL (and their _HI_RES forms) the wheels, and ABS_X/ABS_Y
       * absolute positions on the virtual desktop, scaled from the device's axis ranges. */
      #define WIN32_LEAN_AND_MEAN
      #include "config.h"
      #include <windows.h>
      #include <errno.h>
      #include <stdbool.h>
      #include <stdlib.h>
      #include <string.h>
      #include "libevdev.h"
      #include "libevdev-uinput.h"

      #ifndef LIBEVDEV_EXPORT
      #define LIBEVDEV_EXPORT
      #endif

      #undef open

      struct libevdev_uinput {
          int fd;
          int fd_is_managed;
          char *name;
          struct input_absinfo abs_x, abs_y;
          bool has_abs;
          INPUT pending[64];
          int npending;
          LONG dx, dy, wheel, hwheel, ax, ay;
          bool moved_rel, moved_abs, wheel_hi_res, hwheel_hi_res;
      };

      /* evdev KEY_* codes past the main block: extended scan code (0xE0 prefix) or a virtual key. */
      static const struct { int code; WORD scan; WORD vk; } extended_keys[] = {
          { KEY_KPENTER, 0x1c, 0 }, { KEY_RIGHTCTRL, 0x1d, 0 }, { KEY_KPSLASH, 0x35, 0 }, { KEY_SYSRQ, 0x37, 0 },
          { KEY_RIGHTALT, 0x38, 0 }, { KEY_HOME, 0x47, 0 }, { KEY_UP, 0x48, 0 }, { KEY_PAGEUP, 0x49, 0 },
          { KEY_LEFT, 0x4b, 0 }, { KEY_RIGHT, 0x4d, 0 }, { KEY_END, 0x4f, 0 }, { KEY_DOWN, 0x50, 0 },
          { KEY_PAGEDOWN, 0x51, 0 }, { KEY_INSERT, 0x52, 0 }, { KEY_DELETE, 0x53, 0 }, { KEY_LEFTMETA, 0x5b, 0 },
          { KEY_RIGHTMETA, 0x5c, 0 }, { KEY_COMPOSE, 0x5d, 0 },
          { KEY_PAUSE, 0, VK_PAUSE }, { KEY_MUTE, 0, VK_VOLUME_MUTE }, { KEY_VOLUMEDOWN, 0, VK_VOLUME_DOWN },
          { KEY_VOLUMEUP, 0, VK_VOLUME_UP }, { KEY_NEXTSONG, 0, VK_MEDIA_NEXT_TRACK }, { KEY_PLAYPAUSE, 0, VK_MEDIA_PLAY_PAUSE },
          { KEY_PREVIOUSSONG, 0, VK_MEDIA_PREV_TRACK }, { KEY_STOPCD, 0, VK_MEDIA_STOP }, { KEY_BACK, 0, VK_BROWSER_BACK },
          { KEY_FORWARD, 0, VK_BROWSER_FORWARD }, { KEY_REFRESH, 0, VK_BROWSER_REFRESH }, { KEY_SEARCH, 0, VK_BROWSER_SEARCH },
          { KEY_HOMEPAGE, 0, VK_BROWSER_HOME }, { KEY_BOOKMARKS, 0, VK_BROWSER_FAVORITES }, { KEY_MAIL, 0, VK_LAUNCH_MAIL },
          { KEY_CALC, 0, VK_LAUNCH_APP2 }, { KEY_SLEEP, 0, VK_SLEEP }, { KEY_HELP, 0, VK_HELP },
      };

      static char *dup_string(const char *s)
      {
          size_t n = s != NULL ? strlen(s) + 1 : 1;
          char *d = malloc(n);
          if (d != NULL) memcpy(d, s != NULL ? s : "", n);
          return d;
      }

      LIBEVDEV_EXPORT int libevdev_uinput_create_from_device(const struct libevdev *dev, int uinput_fd,
                                                              struct libevdev_uinput **uinput_dev)
      {
          struct libevdev_uinput *u;
          const struct input_absinfo *abs;
          if (dev == NULL || uinput_dev == NULL) return -EINVAL;
          u = calloc(1, sizeof(*u));
          if (u == NULL) return -ENOMEM;
          if (uinput_fd == LIBEVDEV_UINPUT_OPEN_MANAGED) {
              /* There's no /dev/uinput: the descriptor is a placeholder (the null device) for API users. */
              u->fd = _open("NUL", _O_RDWR | _O_BINARY);
              u->fd_is_managed = 1;
          }
          else u->fd = uinput_fd;
          u->name = dup_string(libevdev_get_name(dev));
          if ((abs = libevdev_get_abs_info(dev, ABS_X)) != NULL) { u->abs_x = *abs; u->has_abs = true; }
          if ((abs = libevdev_get_abs_info(dev, ABS_Y)) != NULL) { u->abs_y = *abs; u->has_abs = true; }
          *uinput_dev = u;
          return 0;
      }

      LIBEVDEV_EXPORT void libevdev_uinput_destroy(struct libevdev_uinput *uinput_dev)
      {
          if (uinput_dev == NULL) return;
          if (uinput_dev->fd_is_managed && uinput_dev->fd >= 0) _close(uinput_dev->fd);
          free(uinput_dev->name);
          free(uinput_dev);
      }

      LIBEVDEV_EXPORT int libevdev_uinput_get_fd(const struct libevdev_uinput *uinput_dev)
      {
          return uinput_dev != NULL ? uinput_dev->fd : -1;
      }

      /* A SendInput device has no sysfs entry or device node. */
      LIBEVDEV_EXPORT const char *libevdev_uinput_get_syspath(struct libevdev_uinput *uinput_dev)
      {
          (void)uinput_dev;
          return NULL;
      }

      LIBEVDEV_EXPORT const char *libevdev_uinput_get_devnode(struct libevdev_uinput *uinput_dev)
      {
          (void)uinput_dev;
          return NULL;
      }

      static INPUT *next_input(struct libevdev_uinput *u)
      {
          INPUT *in;
          if (u->npending == (int)(sizeof(u->pending) / sizeof(u->pending[0]))) {
              SendInput((UINT)u->npending, u->pending, sizeof(INPUT));
              u->npending = 0;
          }
          in = &u->pending[u->npending++];
          memset(in, 0, sizeof(*in));
          return in;
      }

      static void key(struct libevdev_uinput *u, int code, int value)
      {
          INPUT *in;
          size_t i;
          DWORD up = value == 0 ? KEYEVENTF_KEYUP : 0;
          static const DWORD downs[] = { MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_XDOWN, MOUSEEVENTF_XDOWN };
          static const DWORD ups[] = { MOUSEEVENTF_LEFTUP, MOUSEEVENTF_RIGHTUP, MOUSEEVENTF_MIDDLEUP, MOUSEEVENTF_XUP, MOUSEEVENTF_XUP };
          if (code == BTN_TOUCH && u->has_abs) code = BTN_LEFT;
          if (code >= BTN_LEFT && code <= BTN_EXTRA) {
              int button = code - BTN_LEFT;
              in = next_input(u);
              in->type = INPUT_MOUSE;
              in->mi.dwFlags = value ? downs[button] : ups[button];
              if (code == BTN_SIDE) in->mi.mouseData = XBUTTON1;
              if (code == BTN_EXTRA) in->mi.mouseData = XBUTTON2;
              return;
          }
          if (value == 2) value = 1;   /* autorepeat: another key-down */
          for (i = 0; i < sizeof(extended_keys) / sizeof(extended_keys[0]); i++) {
              if (extended_keys[i].code != code) continue;
              in = next_input(u);
              in->type = INPUT_KEYBOARD;
              if (extended_keys[i].vk) {
                  in->ki.wVk = extended_keys[i].vk;
                  in->ki.dwFlags = up;
              }
              else {
                  in->ki.wScan = extended_keys[i].scan;
                  in->ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_EXTENDEDKEY | up;
              }
              return;
          }
          if (code >= KEY_F13 && code <= KEY_F24) {
              in = next_input(u);
              in->type = INPUT_KEYBOARD;
              in->ki.wVk = (WORD)(VK_F13 + (code - KEY_F13));
              in->ki.dwFlags = up;
              return;
          }
          if (code > 0 && code < 0x80) {
              /* evdev's main block is the PC scan code set 1. */
              in = next_input(u);
              in->type = INPUT_KEYBOARD;
              in->ki.wScan = (WORD)code;
              in->ki.dwFlags = KEYEVENTF_SCANCODE | up;
          }
      }

      static LONG scale(const struct input_absinfo *abs, LONG value)
      {
          LONG range = abs->maximum - abs->minimum;
          if (range <= 0) return 0;
          if (value < abs->minimum) value = abs->minimum;
          if (value > abs->maximum) value = abs->maximum;
          return (LONG)(((long long)(value - abs->minimum) * 65535) / range);
      }

      static void flush(struct libevdev_uinput *u)
      {
          INPUT *in;
          if (u->moved_abs) {
              in = next_input(u);
              in->type = INPUT_MOUSE;
              in->mi.dx = scale(&u->abs_x, u->ax);
              in->mi.dy = scale(&u->abs_y, u->ay);
              in->mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
          }
          if (u->moved_rel) {
              in = next_input(u);
              in->type = INPUT_MOUSE;
              in->mi.dx = u->dx;
              in->mi.dy = u->dy;
              in->mi.dwFlags = MOUSEEVENTF_MOVE;
          }
          if (u->wheel) {
              in = next_input(u);
              in->type = INPUT_MOUSE;
              in->mi.mouseData = (DWORD)u->wheel;
              in->mi.dwFlags = MOUSEEVENTF_WHEEL;
          }
          if (u->hwheel) {
              in = next_input(u);
              in->type = INPUT_MOUSE;
              in->mi.mouseData = (DWORD)u->hwheel;
              in->mi.dwFlags = MOUSEEVENTF_HWHEEL;
          }
          u->dx = u->dy = u->wheel = u->hwheel = 0;
          u->moved_abs = u->moved_rel = false;
          u->wheel_hi_res = u->hwheel_hi_res = false;
          if (u->npending > 0) SendInput((UINT)u->npending, u->pending, sizeof(INPUT));
          u->npending = 0;
      }

      LIBEVDEV_EXPORT int libevdev_uinput_write_event(const struct libevdev_uinput *uinput_dev, unsigned int type,
                                                      unsigned int code, int value)
      {
          struct libevdev_uinput *u = (struct libevdev_uinput *)uinput_dev;
          if (u == NULL) return -EINVAL;
          switch (type) {
          case EV_SYN:
              if (code == SYN_REPORT) flush(u);
              else if (code == SYN_DROPPED) { u->npending = 0; u->dx = u->dy = u->wheel = u->hwheel = 0; }
              return 0;
          case EV_KEY:
              if (code >= KEY_CNT) return -EINVAL;
              key(u, (int)code, value);
              return 0;
          case EV_REL:
              switch (code) {
              case REL_X: u->dx += value; u->moved_rel = true; break;
              case REL_Y: u->dy += value; u->moved_rel = true; break;
              case REL_WHEEL: if (!u->wheel_hi_res) u->wheel += value * WHEEL_DELTA; break;
              case REL_HWHEEL: if (!u->hwheel_hi_res) u->hwheel += value * WHEEL_DELTA; break;
              /* 120 units per detent in both evdev's hi-res wheel and Windows' WHEEL_DELTA */
              case REL_WHEEL_HI_RES: if (!u->wheel_hi_res) u->wheel = 0; u->wheel_hi_res = true; u->wheel += value; break;
              case REL_HWHEEL_HI_RES: if (!u->hwheel_hi_res) u->hwheel = 0; u->hwheel_hi_res = true; u->hwheel += value; break;
              }
              return 0;
          case EV_ABS:
              if (code == ABS_X) { u->ax = value; u->moved_abs = true; }
              else if (code == ABS_Y) { u->ay = value; u->moved_abs = true; }
              return 0;
          case EV_MSC:
          case EV_LED:
          case EV_SND:
          case EV_REP:
              return 0;
          }
          return -EINVAL;
      }
    C

    (buildpath/"win/event-names.h").write Utils.safe_popen_read("python3", "libevdev/make-event-names.py",
                                                                 "include/linux/linux/input.h",
                                                                 "include/linux/linux/input-event-codes.h")
    flags = %w[-O2 -std=gnu11 -Iwin -Iinclude/linux -Ilibevdev -I.]
    lib.mkpath
    bin.mkpath
    system ENV.cc, *flags, "-shared", "-o", bin/"libevdev-2.dll",
           "libevdev/libevdev.c", "libevdev/libevdev-names.c", "win/evdev-win.c", "win/libevdev-uinput.c",
           "-Wl,--out-implib,#{lib}/libevdev.dll.a", "-lhid", "-lsetupapi"
    %w[libevdev-events libevdev-list-codes].each do |tool|
      system ENV.cc, *flags, "-o", bin/"#{tool}.exe", "tools/#{tool}.c", "-L#{lib}", "-levdev"
    end

    (include/"libevdev-1.0/libevdev").install "libevdev/libevdev.h", "libevdev/libevdev-uinput.h"
    (include/"libevdev-1.0/linux").install Dir["include/linux/linux/*.h"], "win/linux/types.h"
    (include/"libevdev-1.0/sys").install "win/sys/ioctl.h"
    (lib/"pkgconfig/libevdev.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: libevdev
      Description: Handler library for evdev events (HID devices on Windows)
      Version: #{version}
      Cflags: -I${includedir}/libevdev-1.0
      Libs: -L${libdir} -levdev
    PC
  end

  test do
    (testpath/"test.c").write <<~C
      #include <string.h>
      #include <stddef.h>
      #include <stdio.h>
      #include <libevdev/libevdev.h>

      int main(void) {
        int result = libevdev_new_from_fd(0, NULL);
        printf("%s\\n", strerror(-result));
      }
    C
    system ENV.cc, testpath/"test.c", "-I#{include}/libevdev-1.0", "-L#{lib}", "-levdev", "-o", "test.exe"
    cp bin/"libevdev-2.dll", testpath
    # Standard input isn't an input device.
    assert_equal "Inappropriate I/O control operation", shell_output(testpath/"test.exe").chomp

    (testpath/"names.c").write <<~C
      #include <stdio.h>
      #include <libevdev/libevdev.h>

      int main(void) {
        struct libevdev *dev = libevdev_new();
        libevdev_set_name(dev, "brew test");
        libevdev_enable_event_code(dev, EV_KEY, KEY_A, NULL);
        printf("%s %s %d %d\\n", libevdev_get_name(dev), libevdev_event_code_get_name(EV_KEY, KEY_A),
               libevdev_has_event_code(dev, EV_KEY, KEY_A), libevdev_event_code_from_name(EV_ABS, "ABS_HAT0X"));
        libevdev_free(dev);
        return 0;
      }
    C
    system ENV.cc, testpath/"names.c", "-I#{include}/libevdev-1.0", "-L#{lib}", "-levdev", "-o", "names.exe"
    assert_equal "brew test KEY_A 1 16", shell_output(testpath/"names.exe").chomp
    assert_match "KEY_ESC", shell_output(bin/"libevdev-list-codes")
  end
end
