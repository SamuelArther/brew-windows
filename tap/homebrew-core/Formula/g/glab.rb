class Glab < Formula
  desc "Open-source GitLab command-line tool"
  homepage "https://gitlab.com/gitlab-org/cli"
  url "https://gitlab.com/api/v4/projects/gitlab-org%2Fcli/packages/generic/glab/1.123.0/glab_1.123.0_windows_amd64.zip"
  version "1.123.0"
  sha256 "a3dfe29f617f7d2100ae2aef73cc8ad82fbfee8392ffd99e190a4ba6b6d69d5e"
  license "MIT"

  def install
    bin.install "bin/glab.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/glab --version")
  end
end
