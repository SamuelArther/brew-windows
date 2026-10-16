class Sofka < Formula
  desc "Kubernetes TUI, reimagined in Rust"
  homepage "https://github.com/nklmilojevic/sofka"
  url "https://github.com/nklmilojevic/sofka/releases/download/v0.31.10/sofka-v0.31.10-x86_64-pc-windows-msvc.zip"
  sha256 "fea4782dd17bc8d155cc5b349a109aa4357cafaf19807f6a20ab5ea9749ac910"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nklmilojevic/sofka/archive/refs/tags/v0.31.10.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/sofka.exe", "sofka"
  end

  test do
    assert_equal "sofka #{version}\n", shell_output("#{bin}/sofka --version")
    assert_match "failed to read kubeconfig", shell_output("#{bin}/sofka --check 2>&1", 1)
  end
end
