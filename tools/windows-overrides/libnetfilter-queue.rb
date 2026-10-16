class LibnetfilterQueue < Formula
  desc "Live view of packets passing the network stack (nf-queue on Packet Monitor)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nf-queue", "nf-queue"
  end

  def caveats
    <<~EOS
      nf-queue prints each packet passing the network adapters as it arrives, through Windows'
      Packet Monitor (pktmon, administrator), until Ctrl+C. Windows only lets user space observe
      packets, so there are no verdicts or rewritten payloads; holding packets needs a WFP callout
      driver. There is no libnetfilter_queue C library.
    EOS
  end

  test do
    assert_match "Usage: nf-queue", shell_output("#{bin}/nf-queue --help")
    assert_match "NF_ACCEPT", shell_output("#{bin}/nf-queue --help")
    shell_output("#{bin}/nf-queue not-a-number 2>&1", 2)
  end
end
