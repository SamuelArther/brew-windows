class Bpftop < Formula
  desc "Dynamic real-time view of running eBPF programs (eBPF for Windows)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bpftop", "bpftop"
  end

  def caveats
    <<~EOS
      bpftop lists the programs of eBPF for Windows, whose runtime (drivers and EbpfApi.dll) is installed
      from https://github.com/microsoft/ebpf-for-windows/releases. Run it from an elevated prompt.
    EOS
  end

  test do
    assert_match "bpftop", shell_output("#{bin}/bpftop --version")
    output = shell_output("#{bin}/bpftop 2>&1", 1)
    assert_match(/must be run as root|eBPF for Windows is not installed/, output)
  end
end
