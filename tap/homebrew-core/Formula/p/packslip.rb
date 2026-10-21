class Packslip < Formula
  desc "Signed release manifest for vendor binaries"
  homepage "https://packslip.dev"
  url "https://github.com/jdx/packslip/archive/refs/tags/v1.9.1.tar.gz"
  sha256 "9614dfacd1ce38b16e77bc4b8e05edc3c1aab19b602f9225dac3d55d13b45a23"
  license "MIT"
  head "https://github.com/jdx/packslip.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7355bc4ed41ba743f0634fa9bb1848d154f90befbed992baf38918f13d6e052c"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    generate_completions_from_executable(bin/"packslip", "completion")
    man1.install "packslip.1"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/packslip --version")

    system bin/"packslip", "keygen", "--out", "brew.key"
    (testpath/"brewtest").write "brewed"
    system "tar", "-czf", "brewtest-1.0.0-linux-x64.tar.gz", "brewtest"
    system bin/"packslip", "create", "--project", "example.com/brewtest", "--version", "1.0.0",
           "--key", "brew.key", "--no-log", "--url-base", "https://example.com/1.0.0",
           "--bin", "brewtest", "--out", "dist", "brewtest-1.0.0-linux-x64.tar.gz"

    output = shell_output("#{bin}/packslip verify --pubkey brew.pub --allow-unlogged " \
                          "--artifact brewtest-1.0.0-linux-x64.tar.gz dist/packslip.sigstore.json")
    assert_match "ok: example.com/brewtest 1.0.0", output
  end
end
