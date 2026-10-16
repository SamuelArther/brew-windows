class Vgrep < Formula
  desc "User-friendly pager for grep"
  homepage "https://github.com/vrothberg/vgrep"
  url "https://github.com/vrothberg/vgrep/releases/download/v2.8.0/vgrep_2.8.0_Windows_x86_64.exe"
  sha256 "45fd95cbc7581f9c2b83f6e7fe511f1da11406bfab25c1c169e53ff80c616e05"
  license "GPL-3.0-only"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vrothberg/vgrep/archive/refs/tags/v2.8.0.tar.gz"
    regex(/^v(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "vgrep_2.8.0_Windows_x86_64.exe" => "vgrep.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vgrep --version 2>&1")
  end
end
