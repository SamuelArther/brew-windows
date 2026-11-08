class Desed < Formula
  desc "Debugger for Sed"
  homepage "https://github.com/SoptikHa2/desed"
  url "https://github.com/SoptikHa2/desed/archive/refs/tags/v1.2.3.tar.gz"
  sha256 "10e7836f484d15e545e8c43298ca7f6c6de75278f9778e03ca73877f98c9d25f"
  license "GPL-3.0-or-later"
  head "https://github.com/SoptikHa2/desed.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b636258eaeb0eb2f8effc256180ec1c86725fd8b03cddf9853f75ec0e9b30eee"
  end

  depends_on "rust" => :build
  depends_on "gnu-sed" => :test

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    man1.install "desed.1"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/desed --version")
    # desed is a TUI application
    # Just test that it opens when files are provided
    assert_match "No such file or directory", shell_output("#{bin}/desed test.sed test.txt 2>&1")

    (testpath/"test.txt").write <<~EOS
      1 2 3 4 5 1 2 3 4 5
      232 1 4 526 2 1 1 5
    EOS
    (testpath/"test.sed").write <<~SED
      =
      :bbb
      s/1/2/
      t bbb
      H
      p
      G
      G
      p
    SED

    begin
      pid = spawn bin/"desed", testpath/"test.sed", testpath/"test.txt"
      sleep 2
    ensure
      Process.kill("SIGINT", pid)
      Process.wait(pid)
    end
  end
end
