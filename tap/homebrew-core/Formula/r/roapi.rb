class Roapi < Formula
  desc "Full-fledged APIs for static datasets without writing a single line of code"
  homepage "https://roapi.github.io/docs"
  url "https://github.com/roapi/roapi/archive/refs/tags/roapi-v0.13.0.tar.gz"
  sha256 "f1941e528efac4c610cc042bff7f03a613d7d7b59e2a7f8c10cea8c478d915ee"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0ac9209afe1e630356ea950550a098b4fc0c2ad1f7d084c95cf15785c27ccdaf"
  end

  depends_on "rust" => :build

  # Patch to fix build error, remove in next release
  patch do
    url "https://github.com/roapi/roapi/commit/c53bfa489011038cb934735451d88dcd9f39dbe2.patch?full_index=1"
    sha256 "0bdb9950c1f9e69670283f4fb491f2d71ce0f18248345c38869b4a6615ac823e"
    type :backport
  end

  def install
    # skip default features like snmalloc which errs on ubuntu 16.04
    system "cargo", "install", "--no-default-features", *std_cargo_args(path: "roapi", features: "rustls")
  end

  test do
    # test that versioning works
    assert_equal "roapi #{version}", shell_output("#{bin}/roapi -V").strip

    # test CSV reading + JSON response
    port = free_port
    (testpath/"data.csv").write "name,age\nsam,27\n"
    expected_output = '[{"name":"sam"}]'

    # A drive letter (C:/...) reads as a URI scheme, so Windows passes the table's relative path.
    pid = spawn bin/"roapi", "-a", "localhost:#{port}", "-t", OS.windows? ? "data.csv" : testpath/"data.csv"
    begin
      query = "SELECT name from data"
      header = "ACCEPT: application/json"
      url = "localhost:#{port}/api/sql"
      assert_match expected_output, shell_output("curl -s -X POST -H '#{header}' -d '#{query}' #{url}")
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
