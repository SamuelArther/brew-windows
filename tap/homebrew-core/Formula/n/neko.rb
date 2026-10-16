class Neko < Formula
  desc "High-level, dynamically typed programming language"
  homepage "https://nekovm.org/"
  url "https://github.com/HaxeFoundation/neko/releases/download/v2-4-1/neko-2.4.1-win64.zip"
  sha256 "3902933da42320e8bc04dbee07959ee9ff09a7848e9af48072396400cc3618c9"
  license "MIT"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/HaxeFoundation/neko/archive/refs/tags/v2-4-1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/neko.exe", "neko"
    windows_exe "libexec/nekoc.exe", "nekoc"
    windows_exe "libexec/nekoml.exe", "nekoml"
    windows_exe "libexec/nekotools.exe", "nekotools"
  end

  test do
    ENV["NEKOPATH"] = "#{HOMEBREW_PREFIX}/lib/neko"
    system bin/"neko", "-version"
    (testpath/"hello.neko").write '$print("Hello world!\n");'
    system bin/"nekoc", "hello.neko"
    assert_equal "Hello world!\n", shell_output("#{bin}/neko hello")
  end
end
