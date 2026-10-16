class Carthage < Formula
  desc "Decentralized dependency manager for Cocoa"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "carthage", "carthage"
  end

  def caveats
    <<~EOS
      Dependencies resolve, check out and install prebuilt frameworks on Windows.
      Building Xcode framework schemes needs xcodebuild, which only runs on macOS.
    EOS
  end

  test do
    (testpath/"Cartfile").write 'github "jspahrsummers/xcconfigs"'
    system bin/"carthage", "update"
    assert_match 'github "jspahrsummers/xcconfigs"', (testpath/"Cartfile.resolved").read
    assert_path_exists testpath/"Carthage/Checkouts/xcconfigs/README.md"
    assert_match "No incompatibilities", shell_output("#{bin}/carthage validate")
    assert_match "bootstrap", shell_output("#{bin}/carthage help")
    assert_match "Unrecognized command", shell_output("#{bin}/carthage frobnicate 2>&1", 2)
  end
end
