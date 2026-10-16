class Ord < Formula
  desc "Index, block explorer, and command-line wallet"
  homepage "https://ordinals.com/"
  url "https://github.com/ordinals/ord/releases/download/0.29.0/ord-0.29.0-x86_64-pc-windows-msvc.zip"
  sha256 "93de82db792ccc37ae385c49646c0f649d38049f4e959499c6e7c5d1a81bf2ad"
  license "CC0-1.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ordinals/ord/archive/refs/tags/0.29.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/ord.exe", "ord"
  end

  test do
    output = shell_output("#{bin}/ord list xx:xx 2>&1", 2)
    assert_match "invalid value 'xx:xx' for '<OUTPOINT>': error parsing TXID", output

    assert_match "ord #{version}", shell_output("#{bin}/ord --version")
  end
end
