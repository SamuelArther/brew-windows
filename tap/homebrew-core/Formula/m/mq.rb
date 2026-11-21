class Mq < Formula
  desc "Jq-like command-line tool for markdown processing"
  homepage "https://mqlang.org/"
  url "https://github.com/harehare/mq/archive/refs/tags/v0.10.2.tar.gz"
  sha256 "f760fa96c839400c986d4ff53c25c97f7cf66fcf02725f36fffc097e446ea6c5"
  license "MIT"
  head "https://github.com/harehare/mq.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a44d2dfb510dfd54392427f74864fa65a878261f967467b84bb3108181686359"
  end

  depends_on "rust" => :build

  # Sync stale workspace versions in `Cargo.lock`
  patch do
    url "https://github.com/harehare/mq/commit/3aa2a1fa7a726770bc67b14f1123f205747ce711.patch?full_index=1"
    sha256 "c5a005532626d79b792c47b9cccfe4a9a08d7203904c0c1835a5141ebb57eb84"
    type :backport
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/mq-run")
    system "cargo", "install", *std_cargo_args(path: "crates/mq-lsp")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mq --version")

    (testpath/"test.md").write("# Hello World\n\nThis is a test.")
    output = shell_output("#{bin}/mq '.h' #{testpath}/test.md")
    assert_equal "# Hello World\n", output
  end
end
