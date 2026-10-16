class Intercept < Formula
  desc "Static Application Security Testing (SAST) tool"
  homepage "https://intercept.cc"
  url "https://github.com/xfhg/intercept/releases/download/v1.0.12/intercept-windows-amd64.exe.tgz"
  sha256 "6cdd1ae678a8caae105e7b4fbcbddd7ffab8437aebdfb90a127e136e87bdc23c"
  license "EUPL-1.2"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xfhg/intercept/archive/refs/tags/v1.0.12.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "intercept-windows-amd64.exe" => "intercept.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/intercept version 2>&1")
  end
end
