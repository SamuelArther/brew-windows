class DejaVu < Formula
  desc "Local searchable memory over the session histories of coding agents"
  homepage "https://github.com/vshulcz/deja-vu"
  url "https://github.com/vshulcz/deja-vu/releases/download/v0.22.1/deja-vu_0.22.1_windows_amd64.zip"
  sha256 "a590e45d433fe63954cf2beef2faf664bf374edc6e33fb1ded986745d3efbbc6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vshulcz/deja-vu/archive/refs/tags/v0.22.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/deja.exe", "deja"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/deja version")
    assert_match '"schema_version": 2', shell_output("#{bin}/deja doctor --json --offline")
    assert_match "no matches", shell_output("#{bin}/deja search nothing-is-indexed-here 2>&1")
  end
end
