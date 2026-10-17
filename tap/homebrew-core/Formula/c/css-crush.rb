class CssCrush < Formula
  desc "Extensible PHP based CSS preprocessor"
  homepage "https://the-echoplex.net/csscrush"
  url "https://github.com/peteboere/css-crush/archive/refs/tags/v5.0.1.tar.gz"
  sha256 "98b93ae9acb1586a1bdefeb377065206f62cb5f00f3ac065b65dff9dc5d0a979"
  license "MIT"
  head "https://github.com/peteboere/css-crush.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e4bf94f409ef235c2954ac510e6792396f85ad724e89b85aade44f79af4d51c8"
  end

  depends_on "php"

  deny_network_access!

  def install
    libexec.install Dir["*"]
    (bin/"csscrush").write <<~SHELL
      #!/bin/sh
      php "#{libexec}/cli.php" "$@"
    SHELL
  end

  test do
    (testpath/"test.crush").write <<~EOS
      @define foo #123456;
      p { color: $(foo); }
    EOS

    assert_equal "p{color:#123456}", shell_output("#{bin}/csscrush #{testpath}/test.crush").strip
  end
end
