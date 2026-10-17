class Testscript < Formula
  desc "Integration tests for command-line applications in .txtar format"
  homepage "https://github.com/rogpeppe/go-internal/tree/master/cmd/testscript"
  url "https://github.com/rogpeppe/go-internal/archive/refs/tags/v1.16.0.tar.gz"
  sha256 "78662c2e70976573ee61da4a050d1f10ca495ab35791b7be14d09badab28192f"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2d8f2a5a0a7a28c1ad82ac32d15c4012ef54a197e793cdb098c19111831b7414"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/testscript"
  end

  test do
    (testpath/"hello.txtar").write("exec echo hello!\nstdout hello!")

    assert_equal "PASS\n", shell_output("#{bin}/testscript hello.txtar")
  end
end
