class LinuxHeadersAT44 < Formula
  desc "Header files of the Linux kernel"
  homepage "https://kernel.org/"
  url "https://cdn.kernel.org/pub/linux/kernel/v4.x/linux-4.4.302.tar.gz"
  sha256 "66271f9d9fce8596622e8154ca0ea160e46b78a5a6c967a15b55855f744d1b0b"
  license "GPL-2.0-only" => { with: "Linux-syscall-note" }

  # The kernel's userspace API headers (what `make headers_install` exports, for x86) as a sysroot
  # include/ for cross-compiling Linux programs on Windows (clang --target=x86_64-linux-gnu, zig cc).
  # headers_install needs a Linux build host, so its steps are done here in Ruby: copy the generic
  # and x86 UAPI trees, apply scripts/headers_install.sh's rewrites, generate asm/unistd_*.h from
  # the syscall tables, the asm-generic wrappers, and linux/version.h.

  livecheck do
    skip "No longer developed or maintained"
  end

  keg_only :versioned_formula

  def install
    %w[include/uapi arch/x86/include/uapi].each do |root|
      Dir["#{root}/**/*.h"].each do |path|
        install_header path.delete_prefix("#{root}/"), File.read(path)
      end
    end

    tables = "arch/x86/entry/syscalls"
    install_unistd "unistd_32.h", "#{tables}/syscall_32.tbl", %w[i386], ""
    install_unistd "unistd_64.h", "#{tables}/syscall_64.tbl", %w[common 64], ""
    install_unistd "unistd_x32.h", "#{tables}/syscall_64.tbl", %w[common x32], "__X32_SYSCALL_BIT + "

    # asm/ headers x86 doesn't provide itself are wrappers around the asm-generic ones.
    kbuilds = %w[include/uapi/asm-generic/Kbuild include/uapi/asm-generic/Kbuild.asm arch/x86/include/uapi/asm/Kbuild]
    kbuilds.select { |f| (buildpath/f).exist? }.each do |kbuild|
      File.read(kbuild).scan(/^(?:mandatory|header|generic)-y\s*\+=\s*(\S+\.h)/).flatten.each do |name|
        next if (include/"asm"/name).exist? || !(include/"asm-generic"/name).exist?

        (include/"asm"/name).write "#include <asm-generic/#{name}>\n"
      end
    end

    parts = version.to_s.split(".").map { |v| v.to_i }
    code = (parts[0] * 65536) + (parts[1] * 256) + [parts[2], 255].min
    (include/"linux/version.h").write <<~H
      #define LINUX_VERSION_CODE #{code}
      #define KERNEL_VERSION(a,b,c) (((a) << 16) + ((b) << 8) + ((c) > 255 ? 255 : (c)))
      #define LINUX_VERSION_MAJOR #{parts[0]}
      #define LINUX_VERSION_PATCHLEVEL #{parts[1]}
      #define LINUX_VERSION_SUBLEVEL #{parts[2]}
    H
    doc.install "COPYING"
  end

  # scripts/headers_install.sh: strip kernel-only annotations, spell inline/asm/volatile the way
  # strict C accepts, drop the _UAPI guard prefix, and resolve __EXPORTED_HEADERS__ (unifdef -D).
  def install_header(name, text)
    text = text.gsub(/([ \t(])(__user|__force|__iomem)[ \t]/, "\\1")
    text = text.gsub(/__attribute_const__([ \t]|$)/, "\\1")
    text = text.gsub(%r{^#include <linux/compiler(_types)?\.h>}, "")
    text = text.gsub(/(^|[^a-zA-Z0-9])__packed([^a-zA-Z0-9_]|$)/, "\\1__attribute__((packed))\\2")
    text = text.gsub(/(^|[ \t(])(inline|asm|volatile)([ \t(]|$)/, "\\1__\\2__\\3")
    text = text.gsub(%r{#(ifndef|define|endif[ \t]*/[*])[ \t]*_UAPI}, "#\\1 ")
    text = text.gsub(/^#ifndef[ \t]+__EXPORTED_HEADERS__[ \t]*$/, "#if 0")
    text = text.gsub(/^#ifdef[ \t]+__EXPORTED_HEADERS__[ \t]*$/, "#if 1")
    (include/name).dirname.mkpath
    (include/name).write text
  end

  # scripts/syscallhdr.sh: one __NR_ define per table entry of the given ABIs.
  def install_unistd(name, table, abis, offset)
    guard = "_ASM_#{name.sub(".h", "").upcase}_H"
    defines = []
    File.read(table).each_line do |line|
      fields = line.split
      next if fields.length < 3 || fields[0].start_with?("#") || !abis.include?(fields[1])

      number = offset.empty? ? fields[0] : "(#{offset}#{fields[0]})"
      defines << "#define __NR_#{fields[2]} #{number}"
    end
    text = "#ifndef #{guard}\n#define #{guard} 1\n\n#{defines.join("\n")}\n\n"
    (include/"asm"/name).write "#{text}#endif /* #{guard} */\n"
  end

  def caveats
    <<~EOS
      These are x86 headers. Windows file names ignore case, so the few netfilter headers whose
      names differ only by case (xt_DSCP.h and xt_dscp.h, ipt_ECN.h and ipt_ecn.h, ...) share one file.
    EOS
  end

  test do
    assert_match "KERNEL_VERSION", (include/"linux/version.h").read
    assert_match "#define __NR_openat 257", (include/"asm/unistd_64.h").read
    assert_match "__NR_restart_syscall 0", (include/"asm/unistd_32.h").read
    refute_match "__user", (include/"linux/sysctl.h").read
    assert_path_exists include/"asm-generic/errno-base.h"
    assert_path_exists include/"asm/types.h"
  end
end
