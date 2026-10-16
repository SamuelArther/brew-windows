class Upterm < Formula
  desc "Instant terminal sharing"
  homepage "https://upterm.dev"
  url "https://github.com/owenthereal/upterm/releases/download/v0.38.0/upterm_windows_amd64.tar.gz"
  sha256 "5018c29562341f4982135145b3ef9384e24b7bdf39d4cbc263f9e9ba4db9a698"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/owenthereal/upterm/archive/refs/tags/v0.38.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/upterm.exe", "upterm"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/upterm version 2>&1")
  end
end
