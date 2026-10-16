class ChartTesting < Formula
  desc "Testing and linting Helm charts"
  homepage "https://github.com/helm/chart-testing"
  url "https://github.com/helm/chart-testing/releases/download/v3.15.0/chart-testing_3.15.0_windows_amd64.zip"
  sha256 "6499089288d39d818cc0d49b22593b1167a04d5ee25321940f2b7b4d8903a6d7"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/helm/chart-testing.git"
  end

  def install
    install_tree
    windows_exe "libexec/ct.exe", "ct"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ct version 2>&1")
  end
end
