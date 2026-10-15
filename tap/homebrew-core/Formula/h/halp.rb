class Halp < Formula
  desc "CLI tool to get help with CLI tools"
  homepage "https://halp.cli.rs/"
  url "https://github.com/orhun/halp/releases/download/v0.2.0/halp--x86_64-pc-windows-msvc.zip"
  sha256 "6bf6498a38313c153e5f1e87ffafa9afb74933f517a298edeeaeb25940088d07"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/orhun/halp/archive/refs/tags/v0.2.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/halp.exe", "halp"
  end

  test do
    output = shell_output("#{bin}/halp halp")
    assert_match <<~EOS, output
      (\u00B0\u30ED\u00B0)  checking 'halp -v'
      (\u00D7\uFE4F\u00D7)      fail '-v' argument not found.
      (\u00B0\u30ED\u00B0)  checking 'halp -V'
      \\(^\u30EE^)/ success '-V' argument found!
    EOS

    assert_match version.to_s, shell_output("#{bin}/halp --version")
  end
end
