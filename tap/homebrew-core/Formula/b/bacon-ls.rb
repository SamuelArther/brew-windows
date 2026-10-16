class BaconLs < Formula
  desc "Rust diagnostic provider based on Bacon"
  homepage "https://github.com/crisidev/bacon-ls"
  url "https://github.com/crisidev/bacon-ls/releases/download/0.32.0/bacon-ls-0.32.0-x86_64-pc-windows-msvc.zip"
  sha256 "e7a53dce081d78bdc008772ad2d18325262682823a2c205c2ebbdb125245d199"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/crisidev/bacon-ls/archive/refs/tags/0.32.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/x86_64-pc-windows-msvc/release/bacon-ls.exe", "bacon-ls"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bacon-ls --version 2>&1")
  end
end
