class Libpciaccess < Formula
  desc "Generic PCI access library"
  homepage "https://www.x.org/"
  url "https://www.x.org/pub/individual/lib/libpciaccess-0.19.tar.xz"
  sha256 "3c55aa86c82e54a4e3109786f0463530d53b36b6d1cfd14616454f985dd2aa43"
  license "MIT"

  # libpciaccess has backends for Linux sysfs and the BSDs/Solaris kernels; this adds a Windows one.
  # The device list comes from the Plug and Play manager (SetupAPI/CfgMgr32): IDs, class, revision,
  # bus/device/function, driver names, bridge bus ranges, IRQ and the memory/I/O ranges Windows
  # assigned. Configuration space, BAR mapping, port I/O and the ROM are kernel-only on Windows, so
  # those calls return ENOSYS. Built as libpciaccess-0.dll with the upstream API in pciaccess.h,
  # plus scanpci on top of it.

  livecheck do
    url "https://www.x.org/pub/individual/lib/"
    regex(/href=.*?libpciaccess[._-]v?(\d+\.\d+(?:\.\d+)?)\.t/i)
  end

  def install
    (buildpath/"windows/pciaccess.h").write <<~'C'
      /* libpciaccess API for Windows: PCI devices from the Plug and Play manager (SetupAPI/CfgMgr32).
       * Identification (IDs, class, revision, slot), names (from the drivers' INF files), driver
       * binding and memory/I/O ranges work; configuration space, BAR mapping and the ROM need a kernel
       * driver on Windows, so those calls fail with ENOSYS. */
      #ifndef PCIACCESS_H
      #define PCIACCESS_H

      #include <inttypes.h>

      #if defined(_WIN32) && !defined(PCIACCESS_STATIC)
      #ifdef PCIACCESS_BUILD
      #define PCIACCESS_API __declspec(dllexport)
      #else
      #define PCIACCESS_API __declspec(dllimport)
      #endif
      #else
      #define PCIACCESS_API
      #endif

      #ifndef __deprecated
      #if (__GNUC__ >= 3) || (__STDC_VERSION__ >= 199901L)
      #define __deprecated __attribute__((deprecated))
      #else
      #define __deprecated
      #endif
      #endif

      typedef uint64_t pciaddr_t;

      struct pci_device;
      struct pci_device_iterator;
      struct pci_id_match;
      struct pci_slot_match;

      #ifdef __cplusplus
      extern "C" {
      #endif

      PCIACCESS_API int pci_device_has_kernel_driver(struct pci_device *dev);
      PCIACCESS_API int pci_device_is_boot_vga(struct pci_device *dev);
      PCIACCESS_API int pci_device_read_rom(struct pci_device *dev, void *buffer);
      PCIACCESS_API int __deprecated pci_device_map_region(struct pci_device *dev, unsigned region, int write_enable);
      PCIACCESS_API int __deprecated pci_device_unmap_region(struct pci_device *dev, unsigned region);
      PCIACCESS_API int pci_device_map_range(struct pci_device *dev, pciaddr_t base, pciaddr_t size, unsigned map_flags, void **addr);
      PCIACCESS_API int pci_device_unmap_range(struct pci_device *dev, void *memory, pciaddr_t size);
      PCIACCESS_API int __deprecated pci_device_map_memory_range(struct pci_device *dev, pciaddr_t base, pciaddr_t size, int write_enable, void **addr);
      PCIACCESS_API int __deprecated pci_device_unmap_memory_range(struct pci_device *dev, void *memory, pciaddr_t size);
      PCIACCESS_API int pci_device_probe(struct pci_device *dev);
      PCIACCESS_API const struct pci_agp_info *pci_device_get_agp_info(struct pci_device *dev);
      PCIACCESS_API const struct pci_bridge_info *pci_device_get_bridge_info(struct pci_device *dev);
      PCIACCESS_API const struct pci_pcmcia_bridge_info *pci_device_get_pcmcia_bridge_info(struct pci_device *dev);
      PCIACCESS_API int pci_device_get_bridge_buses(struct pci_device *dev, int *primary_bus, int *secondary_bus, int *subordinate_bus);
      PCIACCESS_API int pci_system_init(void);
      PCIACCESS_API void pci_system_init_dev_mem(int fd);
      PCIACCESS_API void pci_system_cleanup(void);
      PCIACCESS_API struct pci_device_iterator *pci_slot_match_iterator_create(const struct pci_slot_match *match);
      PCIACCESS_API struct pci_device_iterator *pci_id_match_iterator_create(const struct pci_id_match *match);
      PCIACCESS_API void pci_iterator_destroy(struct pci_device_iterator *iter);
      PCIACCESS_API struct pci_device *pci_device_next(struct pci_device_iterator *iter);
      PCIACCESS_API struct pci_device *pci_device_find_by_slot(uint32_t domain, uint32_t bus, uint32_t dev, uint32_t func);
      PCIACCESS_API struct pci_device *pci_device_get_parent_bridge(struct pci_device *dev);
      PCIACCESS_API void pci_get_strings(const struct pci_id_match *m, const char **device_name, const char **vendor_name,
                                         const char **subdevice_name, const char **subvendor_name);
      PCIACCESS_API const char *pci_device_get_device_name(const struct pci_device *dev);
      PCIACCESS_API const char *pci_device_get_subdevice_name(const struct pci_device *dev);
      PCIACCESS_API const char *pci_device_get_vendor_name(const struct pci_device *dev);
      PCIACCESS_API const char *pci_device_get_subvendor_name(const struct pci_device *dev);
      PCIACCESS_API void pci_device_enable(struct pci_device *dev);
      PCIACCESS_API void pci_device_disable(struct pci_device *dev);
      PCIACCESS_API int pci_device_cfg_read(struct pci_device *dev, void *data, pciaddr_t offset, pciaddr_t size, pciaddr_t *bytes_read);
      PCIACCESS_API int pci_device_cfg_read_u8(struct pci_device *dev, uint8_t *data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_read_u16(struct pci_device *dev, uint16_t *data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_read_u32(struct pci_device *dev, uint32_t *data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_write(struct pci_device *dev, const void *data, pciaddr_t offset, pciaddr_t size, pciaddr_t *bytes_written);
      PCIACCESS_API int pci_device_cfg_write_u8(struct pci_device *dev, uint8_t data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_write_u16(struct pci_device *dev, uint16_t data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_write_u32(struct pci_device *dev, uint32_t data, pciaddr_t offset);
      PCIACCESS_API int pci_device_cfg_write_bits(struct pci_device *dev, uint32_t mask, uint32_t data, pciaddr_t offset);

      #ifdef __cplusplus
      }
      #endif

      #define PCI_MATCH_ANY (~0U)

      #define PCI_ID_COMPARE(a, b) (((a) == PCI_MATCH_ANY) || ((a) == (b)))

      struct pci_id_match {
          uint32_t vendor_id;
          uint32_t device_id;
          uint32_t subvendor_id;
          uint32_t subdevice_id;
          uint32_t device_class;
          uint32_t device_class_mask;
          intptr_t match_data;
      };

      struct pci_slot_match {
          uint32_t domain;
          uint32_t bus;
          uint32_t dev;
          uint32_t func;
          intptr_t match_data;
      };

      struct pci_mem_region {
          void *memory;
          pciaddr_t bus_addr;
          pciaddr_t base_addr;
          pciaddr_t size;
          unsigned is_IO:1;
          unsigned is_prefetchable:1;
          unsigned is_64:1;
      };

      struct pci_device {
          uint16_t domain_16;
          uint8_t bus;
          uint8_t dev;
          uint8_t func;
          uint16_t vendor_id;
          uint16_t device_id;
          uint16_t subvendor_id;
          uint16_t subdevice_id;
          uint32_t device_class;
          uint8_t revision;
          struct pci_mem_region regions[6];
          pciaddr_t rom_size;
          int irq;
          intptr_t user_data;
          int vgaarb_rsrc;
          uint32_t domain;
      };

      struct pci_agp_info {
          unsigned config_offset;
          uint8_t major_version;
          uint8_t minor_version;
          uint8_t rates;
          unsigned int fast_writes:1;
          unsigned int addr64:1;
          unsigned int htrans:1;
          unsigned int gart64:1;
          unsigned int coherent:1;
          unsigned int sideband:1;
          unsigned int isochronus:1;
          uint8_t async_req_size;
          uint8_t calibration_cycle_timing;
          uint8_t max_requests;
      };

      struct pci_bridge_info {
          uint8_t primary_bus;
          uint8_t secondary_bus;
          uint8_t subordinate_bus;
          uint8_t secondary_latency_timer;
          uint8_t io_type;
          uint8_t mem_type;
          uint8_t prefetch_mem_type;
          uint16_t secondary_status;
          uint16_t bridge_control;
          uint32_t io_base;
          uint32_t io_limit;
          uint32_t mem_base;
          uint32_t mem_limit;
          uint64_t prefetch_mem_base;
          uint64_t prefetch_mem_limit;
      };

      struct pci_pcmcia_bridge_info {
          uint8_t primary_bus;
          uint8_t card_bus;
          uint8_t subordinate_bus;
          uint8_t cardbus_latency_timer;
          uint16_t secondary_status;
          uint16_t bridge_control;
          struct { uint32_t base; uint32_t limit; } io[2];
          struct { uint32_t base; uint32_t limit; } mem[2];
      };

      #define PCI_DEV_MAP_FLAG_WRITABLE (1U << 0)
      #define PCI_DEV_MAP_FLAG_WRITE_COMBINE (1U << 1)
      #define PCI_DEV_MAP_FLAG_CACHABLE (1U << 2)

      #define VGA_ARB_RSRC_NONE 0x00
      #define VGA_ARB_RSRC_LEGACY_IO 0x01
      #define VGA_ARB_RSRC_LEGACY_MEM 0x02
      #define VGA_ARB_RSRC_NORMAL_IO 0x04
      #define VGA_ARB_RSRC_NORMAL_MEM 0x08

      #ifdef __cplusplus
      extern "C" {
      #endif

      PCIACCESS_API int pci_device_vgaarb_init(void);
      PCIACCESS_API void pci_device_vgaarb_fini(void);
      PCIACCESS_API int pci_device_vgaarb_set_target(struct pci_device *dev);
      PCIACCESS_API int pci_device_vgaarb_decodes(int new_vga_rsrc);
      PCIACCESS_API int pci_device_vgaarb_lock(void);
      PCIACCESS_API int pci_device_vgaarb_trylock(void);
      PCIACCESS_API int pci_device_vgaarb_unlock(void);
      PCIACCESS_API int pci_device_vgaarb_get_info(struct pci_device *dev, int *vga_count, int *rsrc_decodes);

      struct pci_io_handle;
      PCIACCESS_API struct pci_io_handle *pci_device_open_io(struct pci_device *dev, pciaddr_t base, pciaddr_t size);
      PCIACCESS_API struct pci_io_handle *pci_legacy_open_io(struct pci_device *dev, pciaddr_t base, pciaddr_t size);
      PCIACCESS_API void pci_device_close_io(struct pci_device *dev, struct pci_io_handle *handle);
      PCIACCESS_API uint32_t pci_io_read32(struct pci_io_handle *handle, uint32_t reg);
      PCIACCESS_API uint16_t pci_io_read16(struct pci_io_handle *handle, uint32_t reg);
      PCIACCESS_API uint8_t pci_io_read8(struct pci_io_handle *handle, uint32_t reg);
      PCIACCESS_API void pci_io_write32(struct pci_io_handle *handle, uint32_t reg, uint32_t data);
      PCIACCESS_API void pci_io_write16(struct pci_io_handle *handle, uint32_t reg, uint16_t data);
      PCIACCESS_API void pci_io_write8(struct pci_io_handle *handle, uint32_t reg, uint8_t data);
      PCIACCESS_API int pci_device_map_legacy(struct pci_device *dev, pciaddr_t base, pciaddr_t size, unsigned map_flags, void **addr);
      PCIACCESS_API int pci_device_unmap_legacy(struct pci_device *dev, void *addr, pciaddr_t size);

      #ifdef __cplusplus
      }
      #endif

      #endif /* PCIACCESS_H */
    C
    (buildpath/"windows/pciaccess.c").write <<~'C'
      /* libpciaccess for Windows: the PCI device list comes from the Plug and Play manager. Each
       * present device under the PCI enumerator gives its IDs (hardware ID "PCI\VEN_v&DEV_d&SUBSYS_s&REV_r"),
       * class (compatible ID "PCI\CC_ccsspp"), slot (bus number and address properties), names (the
       * driver INF's device description and manufacturer) and the memory/I/O ranges it was assigned. */
      #define PCIACCESS_BUILD
      #include "pciaccess.h"

      #include <errno.h>
      #include <stdlib.h>
      #include <string.h>
      #include <windows.h>
      #include <setupapi.h>
      #include <cfgmgr32.h>

      struct win_device {
          struct pci_device base;
          char *device_name;
          char *vendor_name;
          int has_driver;
          DEVINST devinst;
          struct pci_bridge_info bridge;
          int is_bridge;
      };

      struct pci_device_iterator {
          unsigned next;
          int is_slot;
          struct pci_slot_match slot;
          struct pci_id_match id;
      };

      static struct win_device *devices;
      static unsigned num_devices;

      static char *utf8(const wchar_t *w)
      {
          int n = WideCharToMultiByte(CP_UTF8, 0, w, -1, NULL, 0, NULL, NULL);
          char *s = n > 0 ? (char *)malloc((size_t)n) : NULL;
          if (s) WideCharToMultiByte(CP_UTF8, 0, w, -1, s, n, NULL, NULL);
          return s;
      }

      static int registry_string(HDEVINFO set, SP_DEVINFO_DATA *info, DWORD prop, wchar_t *buf, DWORD bytes)
      {
          DWORD type = 0;
          memset(buf, 0, bytes);
          return SetupDiGetDeviceRegistryPropertyW(set, info, prop, &type, (BYTE *)buf, bytes - sizeof(wchar_t) * 2, NULL);
      }

      static unsigned hex_after(const wchar_t *s, const wchar_t *tag, int digits)
      {
          const wchar_t *p = wcsstr(s, tag);
          unsigned v = 0;
          if (!p) return 0;
          p += wcslen(tag);
          for (int i = 0; i < digits && iswxdigit(p[i]); i++)
              v = v * 16 + (unsigned)(iswdigit(p[i]) ? p[i] - L'0' : (towupper(p[i]) - L'A' + 10));
          return v;
      }

      /* Memory and I/O ranges from the device's allocated configuration, in resource order. */
      static void read_regions(struct win_device *d)
      {
          LOG_CONF conf;
          RES_DES res, next;
          RESOURCEID kind;
          unsigned n = 0;
          if (CM_Get_First_Log_Conf(&conf, d->devinst, ALLOC_LOG_CONF) != CR_SUCCESS
              && CM_Get_First_Log_Conf(&conf, d->devinst, BOOT_LOG_CONF) != CR_SUCCESS)
              return;
          if (CM_Get_Next_Res_Des(&res, conf, ResType_All, &kind, 0) != CR_SUCCESS) {
              CM_Free_Log_Conf_Handle(conf);
              return;
          }
          for (;;) {
              ULONG size = 0;
              if (n < 6 && CM_Get_Res_Des_Data_Size(&size, res, 0) == CR_SUCCESS && size) {
                  BYTE *data = (BYTE *)calloc(1, size);
                  if (data && CM_Get_Res_Des_Data(res, data, size, 0) == CR_SUCCESS) {
                      struct pci_mem_region *r = &d->base.regions[n];
                      if (kind == ResType_Mem) {
                          MEM_DES *m = (MEM_DES *)data;
                          r->base_addr = r->bus_addr = m->MD_Alloc_Base;
                          r->size = m->MD_Alloc_End - m->MD_Alloc_Base + 1;
                          r->is_prefetchable = (m->MD_Flags & fMD_PrefetchAllowed) != 0;
                          r->is_64 = m->MD_Alloc_End > 0xffffffffULL;
                          n++;
                      } else if (kind == ResType_MemLarge) {
                          MEM_LARGE_DES *m = (MEM_LARGE_DES *)data;
                          r->base_addr = r->bus_addr = m->MLD_Alloc_Base;
                          r->size = m->MLD_Alloc_End - m->MLD_Alloc_Base + 1;
                          r->is_prefetchable = (m->MLD_Flags & fMD_PrefetchAllowed) != 0;
                          r->is_64 = 1;
                          n++;
                      } else if (kind == ResType_IO) {
                          IO_DES *io = (IO_DES *)data;
                          r->base_addr = r->bus_addr = io->IOD_Alloc_Base;
                          r->size = io->IOD_Alloc_End - io->IOD_Alloc_Base + 1;
                          r->is_IO = 1;
                          n++;
                      } else if (kind == ResType_IRQ) {
                          LONG irq = (LONG)((IRQ_DES *)data)->IRQD_Alloc_Num;
                          if (irq >= 0) d->base.irq = (int)irq; /* negative numbers are MSI messages */
                      }
                  }
                  free(data);
              }
              int more = CM_Get_Next_Res_Des(&next, res, ResType_All, &kind, 0) == CR_SUCCESS;
              CM_Free_Res_Des_Handle(res);
              if (!more) break;
              res = next;
          }
          CM_Free_Log_Conf_Handle(conf);
      }

      static int compare_slot(const void *a, const void *b)
      {
          const struct pci_device *x = &((const struct win_device *)a)->base, *y = &((const struct win_device *)b)->base;
          uint64_t kx = ((uint64_t)x->domain << 24) | ((uint64_t)x->bus << 16) | (x->dev << 8) | x->func;
          uint64_t ky = ((uint64_t)y->domain << 24) | ((uint64_t)y->bus << 16) | (y->dev << 8) | y->func;
          return kx < ky ? -1 : kx > ky;
      }

      int pci_system_init(void)
      {
          HDEVINFO set;
          SP_DEVINFO_DATA info;
          wchar_t buf[2048];
          unsigned cap = 0;
          if (devices) return 0;
          set = SetupDiGetClassDevsW(NULL, L"PCI", NULL, DIGCF_ALLCLASSES | DIGCF_PRESENT);
          if (set == INVALID_HANDLE_VALUE) return EACCES;
          info.cbSize = sizeof info;
          for (DWORD i = 0; SetupDiEnumDeviceInfo(set, i, &info); i++) {
              struct win_device *d;
              DWORD bus = 0, address = 0, type;
              if (!registry_string(set, &info, SPDRP_HARDWAREID, buf, sizeof buf) || wcsncmp(buf, L"PCI\\VEN_", 8) != 0)
                  continue;
              if (num_devices == cap) {
                  struct win_device *grown = (struct win_device *)realloc(devices, (cap = cap ? cap * 2 : 32) * sizeof *devices);
                  if (!grown) break;
                  devices = grown;
              }
              d = &devices[num_devices];
              memset(d, 0, sizeof *d);
              d->base.vendor_id = (uint16_t)hex_after(buf, L"VEN_", 4);
              d->base.device_id = (uint16_t)hex_after(buf, L"DEV_", 4);
              unsigned subsys = hex_after(buf, L"SUBSYS_", 8);
              d->base.subdevice_id = (uint16_t)(subsys >> 16);
              d->base.subvendor_id = (uint16_t)(subsys & 0xffff);
              d->base.revision = (uint8_t)hex_after(buf, L"REV_", 2);
              if (registry_string(set, &info, SPDRP_COMPATIBLEIDS, buf, sizeof buf)) {
                  for (const wchar_t *id = buf; *id; id += wcslen(id) + 1)
                      if (wcsncmp(id, L"PCI\\CC_", 7) == 0 && wcslen(id) >= 13) {
                          d->base.device_class = hex_after(id, L"CC_", 6);
                          break;
                      }
              }
              if (SetupDiGetDeviceRegistryPropertyW(set, &info, SPDRP_BUSNUMBER, &type, (BYTE *)&bus, sizeof bus, NULL))
                  d->base.bus = (uint8_t)bus;
              if (SetupDiGetDeviceRegistryPropertyW(set, &info, SPDRP_ADDRESS, &type, (BYTE *)&address, sizeof address, NULL)) {
                  d->base.dev = (uint8_t)((address >> 16) & 0x1f);
                  d->base.func = (uint8_t)(address & 0x7);
              }
              /* The PCI segment (domain) shows in the location path as PCIROOT(n) only for root buses;
               * single-segment machines, which is nearly all of them, are domain 0. */
              d->base.domain = 0;
              d->base.domain_16 = 0;
              d->base.irq = -1;
              if (registry_string(set, &info, SPDRP_DEVICEDESC, buf, sizeof buf)) d->device_name = utf8(buf);
              if (registry_string(set, &info, SPDRP_MFG, buf, sizeof buf)) d->vendor_name = utf8(buf);
              d->has_driver = registry_string(set, &info, SPDRP_DRIVER, buf, sizeof buf) && buf[0];
              d->devinst = info.DevInst;
              d->is_bridge = (d->base.device_class >> 8) == 0x0604;
              num_devices++;
          }
          SetupDiDestroyDeviceInfoList(set);
          qsort(devices, num_devices, sizeof *devices, compare_slot);
          for (unsigned i = 0; i < num_devices; i++) read_regions(&devices[i]);
          /* A bridge's secondary bus is the bus its children sit on (children of a devnode in the PnP tree). */
          for (unsigned i = 0; i < num_devices; i++) {
              struct win_device *b = &devices[i];
              if (!b->is_bridge) continue;
              b->bridge.primary_bus = b->base.bus;
              b->bridge.secondary_bus = b->bridge.subordinate_bus = b->base.bus;
              for (unsigned j = 0; j < num_devices; j++) {
                  DEVINST parent;
                  if (CM_Get_Parent(&parent, devices[j].devinst, 0) == CR_SUCCESS && parent == b->devinst) {
                      if (b->bridge.secondary_bus == b->base.bus || devices[j].base.bus < b->bridge.secondary_bus)
                          b->bridge.secondary_bus = devices[j].base.bus;
                      if (devices[j].base.bus > b->bridge.subordinate_bus) b->bridge.subordinate_bus = devices[j].base.bus;
                  }
              }
          }
          return 0;
      }

      void pci_system_init_dev_mem(int fd) { (void)fd; }

      void pci_system_cleanup(void)
      {
          for (unsigned i = 0; i < num_devices; i++) {
              free(devices[i].device_name);
              free(devices[i].vendor_name);
          }
          free(devices);
          devices = NULL;
          num_devices = 0;
      }

      static struct pci_device_iterator *new_iterator(void)
      {
          struct pci_device_iterator *it = (struct pci_device_iterator *)calloc(1, sizeof *it);
          return it;
      }

      struct pci_device_iterator *pci_slot_match_iterator_create(const struct pci_slot_match *match)
      {
          struct pci_device_iterator *it = new_iterator();
          if (!it) return NULL;
          it->is_slot = 1;
          if (match) it->slot = *match;
          else it->slot.domain = it->slot.bus = it->slot.dev = it->slot.func = PCI_MATCH_ANY;
          return it;
      }

      struct pci_device_iterator *pci_id_match_iterator_create(const struct pci_id_match *match)
      {
          struct pci_device_iterator *it = new_iterator();
          if (!it) return NULL;
          if (match) it->id = *match;
          else {
              it->id.vendor_id = it->id.device_id = it->id.subvendor_id = it->id.subdevice_id = PCI_MATCH_ANY;
              it->id.device_class = it->id.device_class_mask = 0;
          }
          return it;
      }

      void pci_iterator_destroy(struct pci_device_iterator *iter) { free(iter); }

      struct pci_device *pci_device_next(struct pci_device_iterator *iter)
      {
          if (!iter) return NULL;
          while (iter->next < num_devices) {
              struct pci_device *d = &devices[iter->next++].base;
              if (iter->is_slot) {
                  if (PCI_ID_COMPARE(iter->slot.domain, d->domain) && PCI_ID_COMPARE(iter->slot.bus, d->bus)
                      && PCI_ID_COMPARE(iter->slot.dev, d->dev) && PCI_ID_COMPARE(iter->slot.func, d->func))
                      return d;
              } else if (PCI_ID_COMPARE(iter->id.vendor_id, d->vendor_id) && PCI_ID_COMPARE(iter->id.device_id, d->device_id)
                         && PCI_ID_COMPARE(iter->id.subvendor_id, d->subvendor_id)
                         && PCI_ID_COMPARE(iter->id.subdevice_id, d->subdevice_id)
                         && (d->device_class & iter->id.device_class_mask) == iter->id.device_class) {
                  return d;
              }
          }
          return NULL;
      }

      struct pci_device *pci_device_find_by_slot(uint32_t domain, uint32_t bus, uint32_t dev, uint32_t func)
      {
          for (unsigned i = 0; i < num_devices; i++) {
              struct pci_device *d = &devices[i].base;
              if (d->domain == domain && d->bus == bus && d->dev == dev && d->func == func) return d;
          }
          return NULL;
      }

      static struct win_device *win(const struct pci_device *dev) { return (struct win_device *)dev; }

      int pci_device_probe(struct pci_device *dev) { return dev ? 0 : EINVAL; }
      int pci_device_has_kernel_driver(struct pci_device *dev) { return dev && win(dev)->has_driver; }

      int pci_device_is_boot_vga(struct pci_device *dev)
      {
          /* The first enabled VGA-class device with a driver: the one Windows booted on. */
          if (!dev || (dev->device_class >> 8) != 0x0300) return 0;
          for (unsigned i = 0; i < num_devices; i++)
              if ((devices[i].base.device_class >> 8) == 0x0300 && devices[i].has_driver)
                  return &devices[i].base == dev;
          return 0;
      }

      const struct pci_bridge_info *pci_device_get_bridge_info(struct pci_device *dev)
      {
          return dev && win(dev)->is_bridge ? &win(dev)->bridge : NULL;
      }

      int pci_device_get_bridge_buses(struct pci_device *dev, int *primary_bus, int *secondary_bus, int *subordinate_bus)
      {
          const struct pci_bridge_info *b = pci_device_get_bridge_info(dev);
          if (!b) return ENODEV;
          *primary_bus = b->primary_bus;
          *secondary_bus = b->secondary_bus;
          *subordinate_bus = b->subordinate_bus;
          return 0;
      }

      const struct pci_agp_info *pci_device_get_agp_info(struct pci_device *dev) { (void)dev; return NULL; }
      const struct pci_pcmcia_bridge_info *pci_device_get_pcmcia_bridge_info(struct pci_device *dev) { (void)dev; return NULL; }

      struct pci_device *pci_device_get_parent_bridge(struct pci_device *dev)
      {
          DEVINST parent;
          if (!dev || CM_Get_Parent(&parent, win(dev)->devinst, 0) != CR_SUCCESS) return NULL;
          for (unsigned i = 0; i < num_devices; i++)
              if (devices[i].devinst == parent && devices[i].is_bridge) return &devices[i].base;
          return NULL;
      }

      const char *pci_device_get_device_name(const struct pci_device *dev) { return dev ? win(dev)->device_name : NULL; }
      const char *pci_device_get_vendor_name(const struct pci_device *dev) { return dev ? win(dev)->vendor_name : NULL; }

      /* Windows keeps no names for subsystems; they are the device's own when the IDs agree. */
      const char *pci_device_get_subdevice_name(const struct pci_device *dev)
      {
          return dev && dev->subdevice_id == dev->device_id && dev->subvendor_id == dev->vendor_id ? win(dev)->device_name : NULL;
      }

      const char *pci_device_get_subvendor_name(const struct pci_device *dev)
      {
          return dev && dev->subvendor_id == dev->vendor_id ? win(dev)->vendor_name : NULL;
      }

      void pci_get_strings(const struct pci_id_match *m, const char **device_name, const char **vendor_name,
                           const char **subdevice_name, const char **subvendor_name)
      {
          const struct pci_device *found = NULL;
          for (unsigned i = 0; i < num_devices && !found; i++) {
              const struct pci_device *d = &devices[i].base;
              if (PCI_ID_COMPARE(m->vendor_id, d->vendor_id) && PCI_ID_COMPARE(m->device_id, d->device_id)) found = d;
          }
          if (device_name) *device_name = found && m->device_id != PCI_MATCH_ANY ? pci_device_get_device_name(found) : NULL;
          if (vendor_name) *vendor_name = found ? pci_device_get_vendor_name(found) : NULL;
          if (subdevice_name) *subdevice_name = found ? pci_device_get_subdevice_name(found) : NULL;
          if (subvendor_name) *subvendor_name = found ? pci_device_get_subvendor_name(found) : NULL;
      }

      /* Memory and I/O decoding is switched on by the device's driver when Windows starts it, and the
       * command register is kernel-only, so there is nothing for these to do. */
      void pci_device_enable(struct pci_device *dev) { (void)dev; }
      void pci_device_disable(struct pci_device *dev) { (void)dev; }

      /* Configuration space, BAR mapping, port I/O and the option ROM are kernel-only on Windows. */
      int pci_device_read_rom(struct pci_device *dev, void *buffer) { (void)dev; (void)buffer; return ENOSYS; }
      int pci_device_map_region(struct pci_device *dev, unsigned region, int write_enable) { (void)dev; (void)region; (void)write_enable; return ENOSYS; }
      int pci_device_unmap_region(struct pci_device *dev, unsigned region) { (void)dev; (void)region; return ENOSYS; }
      int pci_device_map_range(struct pci_device *dev, pciaddr_t base, pciaddr_t size, unsigned map_flags, void **addr)
      { (void)dev; (void)base; (void)size; (void)map_flags; if (addr) *addr = NULL; return ENOSYS; }
      int pci_device_unmap_range(struct pci_device *dev, void *memory, pciaddr_t size) { (void)dev; (void)memory; (void)size; return ENOSYS; }
      int pci_device_map_memory_range(struct pci_device *dev, pciaddr_t base, pciaddr_t size, int write_enable, void **addr)
      { return pci_device_map_range(dev, base, size, write_enable ? PCI_DEV_MAP_FLAG_WRITABLE : 0, addr); }
      int pci_device_unmap_memory_range(struct pci_device *dev, void *memory, pciaddr_t size) { return pci_device_unmap_range(dev, memory, size); }
      int pci_device_map_legacy(struct pci_device *dev, pciaddr_t base, pciaddr_t size, unsigned map_flags, void **addr)
      { return pci_device_map_range(dev, base, size, map_flags, addr); }
      int pci_device_unmap_legacy(struct pci_device *dev, void *addr, pciaddr_t size) { return pci_device_unmap_range(dev, addr, size); }

      int pci_device_cfg_read(struct pci_device *dev, void *data, pciaddr_t offset, pciaddr_t size, pciaddr_t *bytes_read)
      { (void)dev; (void)data; (void)offset; (void)size; if (bytes_read) *bytes_read = 0; return ENOSYS; }
      int pci_device_cfg_read_u8(struct pci_device *dev, uint8_t *data, pciaddr_t offset) { return pci_device_cfg_read(dev, data, offset, 1, NULL); }
      int pci_device_cfg_read_u16(struct pci_device *dev, uint16_t *data, pciaddr_t offset) { return pci_device_cfg_read(dev, data, offset, 2, NULL); }
      int pci_device_cfg_read_u32(struct pci_device *dev, uint32_t *data, pciaddr_t offset) { return pci_device_cfg_read(dev, data, offset, 4, NULL); }
      int pci_device_cfg_write(struct pci_device *dev, const void *data, pciaddr_t offset, pciaddr_t size, pciaddr_t *bytes_written)
      { (void)dev; (void)data; (void)offset; (void)size; if (bytes_written) *bytes_written = 0; return ENOSYS; }
      int pci_device_cfg_write_u8(struct pci_device *dev, uint8_t data, pciaddr_t offset) { return pci_device_cfg_write(dev, &data, offset, 1, NULL); }
      int pci_device_cfg_write_u16(struct pci_device *dev, uint16_t data, pciaddr_t offset) { return pci_device_cfg_write(dev, &data, offset, 2, NULL); }
      int pci_device_cfg_write_u32(struct pci_device *dev, uint32_t data, pciaddr_t offset) { return pci_device_cfg_write(dev, &data, offset, 4, NULL); }
      int pci_device_cfg_write_bits(struct pci_device *dev, uint32_t mask, uint32_t data, pciaddr_t offset)
      { (void)dev; (void)mask; (void)data; (void)offset; return ENOSYS; }

      struct pci_io_handle *pci_device_open_io(struct pci_device *dev, pciaddr_t base, pciaddr_t size) { (void)dev; (void)base; (void)size; errno = ENOSYS; return NULL; }
      struct pci_io_handle *pci_legacy_open_io(struct pci_device *dev, pciaddr_t base, pciaddr_t size) { return pci_device_open_io(dev, base, size); }
      void pci_device_close_io(struct pci_device *dev, struct pci_io_handle *handle) { (void)dev; (void)handle; }
      uint32_t pci_io_read32(struct pci_io_handle *handle, uint32_t reg) { (void)handle; (void)reg; return 0xffffffffu; }
      uint16_t pci_io_read16(struct pci_io_handle *handle, uint32_t reg) { (void)handle; (void)reg; return 0xffff; }
      uint8_t pci_io_read8(struct pci_io_handle *handle, uint32_t reg) { (void)handle; (void)reg; return 0xff; }
      void pci_io_write32(struct pci_io_handle *handle, uint32_t reg, uint32_t data) { (void)handle; (void)reg; (void)data; }
      void pci_io_write16(struct pci_io_handle *handle, uint32_t reg, uint16_t data) { (void)handle; (void)reg; (void)data; }
      void pci_io_write8(struct pci_io_handle *handle, uint32_t reg, uint8_t data) { (void)handle; (void)reg; (void)data; }

      /* There is one display stack on Windows and no VGA arbiter to take the legacy ranges from. */
      int pci_device_vgaarb_init(void) { return 0; }
      void pci_device_vgaarb_fini(void) {}
      int pci_device_vgaarb_set_target(struct pci_device *dev) { (void)dev; return 0; }
      int pci_device_vgaarb_decodes(int new_vga_rsrc) { (void)new_vga_rsrc; return 0; }
      int pci_device_vgaarb_lock(void) { return 0; }
      int pci_device_vgaarb_trylock(void) { return 0; }
      int pci_device_vgaarb_unlock(void) { return 0; }
      int pci_device_vgaarb_get_info(struct pci_device *dev, int *vga_count, int *rsrc_decodes)
      {
          int count = 0;
          for (unsigned i = 0; i < num_devices; i++)
              if ((devices[i].base.device_class >> 8) == 0x0300) count++;
          if (vga_count) *vga_count = count;
          if (rsrc_decodes) *rsrc_decodes = dev && pci_device_is_boot_vga(dev) ? VGA_ARB_RSRC_LEGACY_IO | VGA_ARB_RSRC_LEGACY_MEM : VGA_ARB_RSRC_NONE;
          return 0;
      }
    C
    (buildpath/"windows/scanpci.c").write <<~'C'
      /* scanpci: list PCI devices through libpciaccess, in the upstream tool's format. Configuration
       * space isn't readable from user mode on Windows, so STATUS/COMMAND and the -v dumps are absent. */
      #include <pciaccess.h>
      #include <stdio.h>
      #include <string.h>

      int main(int argc, char *argv[])
      {
          struct pci_device_iterator *iter;
          struct pci_device *dev;
          int err;
          for (int i = 1; i < argc; i++) {
              if (strcmp(argv[i], "-h") == 0 || strcmp(argv[i], "--help") == 0) {
                  puts("usage: scanpci [-v]\nList PCI devices (vendor, device, class, assigned memory and I/O ranges).");
                  return 0;
              }
              if (strcmp(argv[i], "-v") != 0) {
                  fprintf(stderr, "usage: scanpci [-v]\n");
                  return 1;
              }
          }
          err = pci_system_init();
          if (err) {
              fprintf(stderr, "Couldn't initialize PCI system: %s\n", strerror(err));
              return 1;
          }
          iter = pci_slot_match_iterator_create(NULL);
          while ((dev = pci_device_next(iter)) != NULL) {
              const char *vendor = pci_device_get_vendor_name(dev), *name = pci_device_get_device_name(dev);
              pci_device_probe(dev);
              printf("\npci ");
              if (dev->domain != 0) printf("domain 0x%04x ", dev->domain);
              printf("bus 0x%04x cardnum 0x%02x function 0x%02x: vendor 0x%04x device 0x%04x\n",
                     dev->bus, dev->dev, dev->func, dev->vendor_id, dev->device_id);
              if (vendor || name) printf(" %s %s\n", vendor ? vendor : "Vendor unknown", name ? name : "Device unknown");
              else printf(" Device unknown\n");
              if (dev->subvendor_id != 0 || dev->subdevice_id != 0) {
                  const char *sv = pci_device_get_subvendor_name(dev), *sd = pci_device_get_subdevice_name(dev);
                  printf(" CardVendor 0x%04x card 0x%04x (%s, %s)\n", dev->subvendor_id, dev->subdevice_id,
                         sv ? sv : "Card vendor unknown", sd ? sd : "Card unknown");
              }
              printf("  CLASS     0x%02x 0x%02x 0x%02x  REVISION 0x%02x\n", (dev->device_class >> 16) & 0xff,
                     (dev->device_class >> 8) & 0xff, dev->device_class & 0xff, dev->revision);
              for (int i = 0; i < 6; i++) {
                  const struct pci_mem_region *r = &dev->regions[i];
                  if (r->size == 0) continue;
                  printf("  BASE%d     0x%0*llx SIZE %llu  %s%s\n", i, r->is_64 ? 16 : 8, (unsigned long long)r->base_addr,
                         (unsigned long long)r->size, r->is_IO ? "I/O" : "MEM", r->is_prefetchable ? " PREFETCHABLE" : "");
              }
              const struct pci_bridge_info *b = pci_device_get_bridge_info(dev);
              if (b) printf("  BRIDGE    primary %u  secondary %u  subordinate %u\n", b->primary_bus, b->secondary_bus, b->subordinate_bus);
          }
          pci_iterator_destroy(iter);
          pci_system_cleanup();
          return 0;
      }
    C
    cd "windows" do
      system ENV.cc, "-O2", "-shared", "-I.", "-o", "libpciaccess-0.dll", "pciaccess.c",
             "-Wl,--out-implib,libpciaccess.dll.a", "-lsetupapi", "-lcfgmgr32"
      system ENV.cc, "-O2", "-I.", "-o", "scanpci.exe", "scanpci.c", "-L.", "-lpciaccess"
      include.install "pciaccess.h"
      lib.install "libpciaccess.dll.a"
      bin.install "libpciaccess-0.dll", "scanpci.exe"
    end
    (lib/"pkgconfig/pciaccess.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: pciaccess
      Description: Library providing generic access to the PCI bus and devices (Windows PnP backend)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -lpciaccess
    PC
    doc.install "COPYING"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # run scanpci, which uses the library, instead.
    output = shell_output(bin/"scanpci")
    assert_match(/pci bus 0x[0-9a-f]{4} cardnum/, output) unless output.strip.empty?
    assert_match "pci_device_next", (include/"pciaccess.h").read
    assert_path_exists lib/"libpciaccess.dll.a"
    assert_match "usage: scanpci", shell_output("#{bin}/scanpci --help")
  end
end
