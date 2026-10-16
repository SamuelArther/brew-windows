class Numactl < Formula
  desc "NUMA support for Windows: node topology, CPU and memory-node binding"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "numactl", "numactl"
    windows_exe "libexec/wintools.exe", "numastat", "numastat"
    windows_exe "libexec/wintools.exe", "memhog", "memhog"
  end

  def caveats
    <<~EOS
      --membind/--preferred set Windows' preferred memory node (a strong preference, not a
      hard bind); CPU binding stays inside one processor group; --interleave is not available.
      There is no libnuma: C programs use the Windows NUMA API (VirtualAllocExNuma and friends).
    EOS
  end

  test do
    assert_match "available:", shell_output("#{bin}/numactl --hardware")
    assert_match "node 0 cpus:", shell_output("#{bin}/numactl -H")
    assert_match "physcpubind:", shell_output("#{bin}/numactl --show")
    assert_match "bound", shell_output("#{bin}/numactl --cpunodebind=0 --membind=0 cmd /c echo bound")
    shell_output("#{bin}/numactl -N 0 -- cmd /c exit 3", 3)
    assert_match "MemTotal", shell_output("#{bin}/numastat")
    shell_output("#{bin}/memhog -r1 4m --membind 0")
    shell_output("#{bin}/numactl --physcpubind=99999 cmd 2>&1", 2)
  end
end
