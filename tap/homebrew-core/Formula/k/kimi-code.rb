class KimiCode < Formula
  desc "AI coding agent for your terminal"
  homepage "https://moonshotai.github.io/kimi-code/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-kimi-code-2.1.1-1-any.pkg.tar.zst"
  sha256 "9f11145619ba86d03e7522fdb2ab1f797571ee722ae420df7e3b49974963fdd1"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://registry.npmjs.org/@moonshot-ai/kimi-code/-/kimi-code-2.2.0.tgz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"kimi"
  end
end
