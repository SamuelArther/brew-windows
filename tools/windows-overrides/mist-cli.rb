class MistCli < Formula
  desc "Command-line tool that downloads macOS firmwares and installers"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mist", "mist"
  end

  def caveats
    <<~EOS
      On Windows the installer "package" output (InstallAssistant.pkg) and firmware .ipsw downloads work;
      building an .app, disk image, ISO or bootable installer needs a Mac.
    EOS
  end

  test do
    assert_match "-h, --help", shell_output("#{bin}/mist").strip
    out = testpath/"out.json"
    system bin/"mist", "list", "firmware", "--quiet", "--export=#{out}", "--output-type=json"
    assert_path_exists out
    parsed = JSON.parse(File.read(out))
    assert_kind_of Array, parsed
    assert_match "unknown platform type", shell_output("#{bin}/mist list nonsense 2>&1", 2)
  end
end
