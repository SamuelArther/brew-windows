class QwenCode < Formula
  desc "AI-powered command-line workflow tool for developers"
  homepage "https://github.com/QwenLM/qwen-code"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qwen-code-0.24.7-1-any.pkg.tar.zst"
  sha256 "939df75a01fc190fd83a59052daf25d857e06c9b52e293fe82ca3a16cc15130e"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://registry.npmjs.org/@qwen-code/qwen-code/-/qwen-code-0.25.1.tgz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"qwen"
  end
end
