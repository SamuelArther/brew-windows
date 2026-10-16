class Libseccomp < Formula
  desc "Linux syscall name and number resolver (scmp_sys_resolver)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "scmp_sys_resolver", "scmp_sys_resolver"
  end

  def caveats
    <<~EOS
      scmp_sys_resolver resolves Linux system calls for x86_64, x32, x86, aarch64 and riscv64, for
      writing seccomp profiles on Windows. There is no libseccomp C library: Windows has no
      per-syscall filter; its nearest is process mitigation policies (SetProcessMitigationPolicy).
    EOS
  end

  test do
    assert_equal "257", shell_output("#{bin}/scmp_sys_resolver -a x86_64 openat").strip
    assert_equal "openat", shell_output("#{bin}/scmp_sys_resolver -a aarch64 56").strip
    assert_equal "102", shell_output("#{bin}/scmp_sys_resolver -a x86 -t socket").strip
    assert_equal "-1", shell_output("#{bin}/scmp_sys_resolver -a x86_64 no_such_call").strip
    assert_match "usage: scmp_sys_resolver", shell_output("#{bin}/scmp_sys_resolver --help")
    shell_output("#{bin}/scmp_sys_resolver -a vax read 2>&1", 2)
  end
end
