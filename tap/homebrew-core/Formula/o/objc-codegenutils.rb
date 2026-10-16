class ObjcCodegenutils < Formula
  desc "Three small tools to help work with Xcode"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "objc-assetgen", "objc-assetgen"
    windows_exe "libexec/wintools.exe", "objc-colordump", "objc-colordump"
    windows_exe "libexec/wintools.exe", "objc-identifierconstants", "objc-identifierconstants"
  end

  test do
    system bin/"objc-assetgen", "-h"
    system bin/"objc-colordump", "-h"
    system bin/"objc-identifierconstants", "-h"
    (testpath/"Foo.xcassets/Big Button.imageset").mkpath
    system bin/"objc-assetgen", "-o", testpath/"out", testpath/"Foo.xcassets"
    assert_match "+ (UIImage *)bigButtonImage;", (testpath/"out/FooCatalog.h").read
    (testpath/"Main.storyboard").write '<document><scenes><scene><objects><viewController storyboardIdentifier="login" id="a"/></objects></scene></scenes></document>'
    system bin/"objc-identifierconstants", "-o", testpath/"out", testpath/"Main.storyboard"
    assert_match "MainStoryboardLoginIdentifier = @\"login\"", (testpath/"out/MainStoryboardIdentifiers.m").read
  end
end
