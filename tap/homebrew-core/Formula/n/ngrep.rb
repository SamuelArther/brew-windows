class Ngrep < Formula
  desc "Network grep"
  homepage "https://github.com/jpr5/ngrep"
  url "https://github.com/jpr5/ngrep/releases/download/v1.49.0/ngrep-windows-x86_64.zip"
  sha256 "9fd10529e25d0a8a572155e7d33c8bdd54db744a18c709bc34a20020913db95f"
  license "ngrep"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jpr5/ngrep/archive/refs/tags/v1.49.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/ngrep.exe", "ngrep"
  end

  test do
    assert_path_exists bin/"ngrep.exe"
  end
end
