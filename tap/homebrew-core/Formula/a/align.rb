class Align < Formula
  desc "Text column alignment filter"
  homepage "https://kinzler.com/me/align/"
  url "https://kinzler.com/me/align/align-1.7.5.tgz"
  sha256 "cc692fb9dee0cc288757e708fc1a3b6b56ca1210ca181053a371cb11746969dd"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?align[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "37a934b09ec1677ed428700ca9e3e6b8c47cd0a6cc0a9be6f976441abcb1954f"
  end

  conflicts_with "speech-tools", because: "both install `align` binaries"

  deny_network_access!

  def install
    system "make", "install", "BINDIR=#{bin}"
  end

  test do
    assert_equal " 1  1\n12 12\n", pipe_output(bin/"align", "1 1\n12 12\n", 0)
  end
end
