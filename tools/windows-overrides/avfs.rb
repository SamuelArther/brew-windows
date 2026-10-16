class Avfs < Formula
  desc "Virtual file system that facilitates looking inside archives (on ProjFS)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "avfsd", "avfsd"
    windows_exe "libexec/wintools.exe", "mountavfs", "mountavfs"
    windows_exe "libexec/wintools.exe", "umountavfs", "umountavfs"
  end

  def caveats
    <<~EOS
      AVFS needs the Windows Projected File System. Turn it on once (as admin) with:
        Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart
      Then `mountavfs` and browse ~/.avfs/C/path/to/archive.zip#/
    EOS
  end

  test do
    system bin/"avfsd", "--version"
    assert_match "AVFS version", shell_output("#{bin}/avfsd --version")
    assert_match "missing mountpoint", shell_output("#{bin}/avfsd 2>&1", 2)
    ENV["AVFSBASE"] = (testpath/"avfs").to_s
    assert_match "AVFS not mounted", shell_output("#{bin}/umountavfs")
  end
end
