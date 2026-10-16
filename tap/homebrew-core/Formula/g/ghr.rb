class Ghr < Formula
  desc "Upload multiple artifacts to GitHub Release in parallel"
  homepage "https://github.com/tcnksm/ghr"
  url "https://github.com/tcnksm/ghr/releases/download/v0.18.5/ghr_v0.18.5_windows_amd64.zip"
  sha256 "7a490d4d91226f65636de0621029fe0f7ccc99742e89d68363f1966e3aa81816"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tcnksm/ghr/archive/refs/tags/v0.18.5.tar.gz"
  end

  def install
    bin.install "ghr.exe"
  end

  test do
    ENV["GITHUB_TOKEN"] = nil
    args = "-username testbot -repository #{testpath} v#{version} #{Dir.pwd}"
    assert_includes "token not found", shell_output("#{bin}/ghr #{args}", 15)
  end
end
