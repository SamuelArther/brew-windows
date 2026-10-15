class Volta < Formula
  desc "JavaScript toolchain manager for reproducible environments"
  homepage "https://volta.sh"
  url "https://github.com/volta-cli/volta/releases/download/v2.0.2/volta-2.0.2-windows.zip"
  sha256 "d7678a485e2fc9b1a3f7e9da256d8b88a97d7e4c45e63701c4874f6c2a587e0f"
  license "BSD-2-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/volta.exe", "volta"
    windows_exe "libexec/volta-shim.exe", "volta-shim"
    windows_exe "libexec/volta-migrate.exe", "volta-migrate"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/volta --version")
  end
end
