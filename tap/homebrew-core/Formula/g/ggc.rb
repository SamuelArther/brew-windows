class Ggc < Formula
  desc "Modern Git CLI"
  homepage "https://github.com/bmf-san/ggc"
  url "https://github.com/bmf-san/ggc/releases/download/v8.7.4/ggc_8.7.4_windows_amd64.zip"
  sha256 "6040a34b2271791be030d74d7a75ff83401dec096f530cdaad32d03e24fc2d00"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bmf-san/ggc/archive/refs/tags/v8.7.4.tar.gz"
  end

  def install
    bin.install "ggc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ggc version")
    assert_equal "main", shell_output("#{bin}/ggc config get default.branch").chomp
  end
end
