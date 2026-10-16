class Libbpf < Formula
  desc "Berkeley Packet Filter library (eBPF for Windows' libbpf API)"
  homepage "https://github.com/microsoft/ebpf-for-windows"
  url "https://api.nuget.org/v3-flatcontainer/ebpf-for-windows.x64/1.6.0/ebpf-for-windows.x64.1.6.0.nupkg"
  version "1.6.0"
  sha256 "79d07fa2929548b692eaca1982c7ce113eaa80402c46ce4a79cc11a581082c70"
  license "MIT"

  # Linux's libbpf talks to the kernel's bpf() system call. On Windows the eBPF runtime is Microsoft's
  # eBPF for Windows, whose user-mode library EbpfApi.dll implements libbpf's API (bpf_object__open,
  # bpf_prog_load, maps, links, ring buffers, bpf_obj_get_info_by_fd ...) over its kernel driver.
  # This is its official SDK package: the libbpf headers (<bpf/libbpf.h>, <bpf/bpf.h>), EbpfApi.lib
  # for MSVC, and bpf2c, which compiles eBPF ELF objects into native Windows drivers.
  # For MinGW compilers the keg adds libbpf.dll.a (an import library for EbpfApi.dll under libbpf's
  # name) with libbpf's version functions, which EbpfApi.dll doesn't export, and lets the SDK
  # headers' host-side branches (written for MSVC) apply to any Windows compiler.

  livecheck do
    url "https://api.nuget.org/v3-flatcontainer/ebpf-for-windows.x64/index.json"
    regex(/"v?(\d+(?:\.\d+)+)"/i)
  end

  # Exported names of a PE DLL, read from its export directory.
  def dll_exports(path)
    bytes = File.binread(path).bytes
    u32 = ->(at) { bytes[at] | (bytes[at + 1] << 8) | (bytes[at + 2] << 16) | (bytes[at + 3] << 24) }
    pe = u32.call(0x3c)
    optional = pe + 24
    first = optional + bytes[pe + 20] + (bytes[pe + 21] << 8)
    sections = (0...(bytes[pe + 6] + (bytes[pe + 7] << 8))).map do |i|
      header = first + (i * 40)
      [u32.call(header + 12), u32.call(header + 8), u32.call(header + 20)]
    end
    file_offset = lambda do |rva|
      section = sections.find { |entry| rva >= entry[0] && rva < entry[0] + entry[1] }
      rva - section[0] + section[2]
    end
    exports = file_offset.call(u32.call(optional + 112))
    names = file_offset.call(u32.call(exports + 32))
    (0...u32.call(exports + 24)).map do |i|
      start = file_offset.call(u32.call(names + (i * 4)))
      finish = start
      finish += 1 while bytes[finish] != 0
      bytes[start...finish].pack("C*")
    end
  end

  def install
    sdk = buildpath/"build/native"
    install_tree "build/native/include", "include"

    # The SDK's headers pick their user-mode (host) declarations with `#ifdef _MSC_VER` and otherwise
    # assume an eBPF program is being compiled (clang -target bpf, which never defines _WIN32).
    %w[linux/bpf.h ebpf_structs.h ebpf_windows.h ebpf_nethooks.h net/if_ether.h net/ip.h].each do |name|
      header = include/name
      text = header.read
      text = text.gsub(/^#ifdef _MSC_VER\r?$/, "#ifdef _WIN32")
                 .gsub(/^#if defined\(_MSC_VER\)\r?$/, "#if defined(_WIN32)")
      # MSVC has no pid_t (the CRT's own is used on MinGW); static_assert needs <assert.h> in C.
      text = text.sub(/^typedef uint32_t pid_t;\r?$/, "#ifndef _WIN32\ntypedef uint32_t pid_t;\n#endif")
      if name == "ebpf_windows.h"
        text = text.sub(/^#pragma once\r?$/, "#pragma once\n#ifdef _WIN32\n#include <assert.h>\n#endif")
      end
      header.atomic_write text
    end

    (buildpath/"libbpf_version.c").write <<~C
      /* libbpf's version functions, for the libbpf API level eBPF for Windows implements. */
      #include <stdio.h>
      #include <bpf/libbpf.h>

      __u32 libbpf_major_version(void) { return LIBBPF_MAJOR_VERSION; }
      __u32 libbpf_minor_version(void) { return LIBBPF_MINOR_VERSION; }

      const char *libbpf_version_string(void)
      {
          static char version[16];
          snprintf(version, sizeof(version), "v%u.%u", (unsigned)LIBBPF_MAJOR_VERSION, (unsigned)LIBBPF_MINOR_VERSION);
          return version;
      }
    C
    system ENV.cc, "-O2", "-w", "-I#{include}", "-c", "libbpf_version.c", "-o", "libbpf_version.o"

    exports = dll_exports(sdk/"bin/EbpfApi.dll").reject { |name| name.start_with?("?") }
    (buildpath/"libbpf.def").write "LIBRARY EbpfApi.dll\nEXPORTS\n#{exports.join("\n")}\n"
    lib.mkpath
    system "dlltool", "-d", "libbpf.def", "-D", "EbpfApi.dll", "-l", lib/"libbpf.dll.a"
    system "ar", "rcs", lib/"libbpf.dll.a", "libbpf_version.o"
    lib.install "build/native/lib/EbpfApi.lib"

    (lib/"pkgconfig/libbpf.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: libbpf
      Description: BPF library (eBPF for Windows)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -lbpf
    PC

    install_tree "build/native/bin", "libexec/bin"
    windows_exe "libexec/bin/bpf2c.exe", "bpf2c"
    pkgshare.install "build/native/bin/Convert-BpfToNative.ps1"
  end

  def caveats
    <<~EOS
      Programs that load eBPF programs need the eBPF for Windows runtime (its drivers and
      EbpfApi.dll), installed from the MSI on its releases page:
        https://github.com/microsoft/ebpf-for-windows/releases
    EOS
  end

  test do
    (testpath/"test.c").write <<~C
      #include "bpf/libbpf.h"
      #include <stdio.h>

      int main() {
        printf("%s", libbpf_version_string());
        return(0);
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lbpf", "-o", "test.exe"
    assert_match(/^v\d+\.\d+$/, shell_output("./test.exe"))

    # The rest of the API resolves against EbpfApi.dll.
    (testpath/"ids.c").write <<~C
      #include "bpf/bpf.h"
      #include <stdio.h>

      int main() {
        __u32 id = 0;
        while (bpf_prog_get_next_id(id, &id) == 0) printf("%u\\n", id);
        return 0;
      }
    C
    system ENV.cc, "ids.c", "-I#{include}", "-L#{lib}", "-lbpf", "-o", "ids.exe"
    assert_path_exists testpath/"ids.exe"
    assert_path_exists libexec/"bin/bpf2c.exe"
    assert_match "-lbpf", (lib/"pkgconfig/libbpf.pc").read
  end
end
