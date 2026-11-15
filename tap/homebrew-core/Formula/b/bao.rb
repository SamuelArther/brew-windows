class Bao < Formula
  desc "Implementation of BLAKE3 verified streaming"
  homepage "https://github.com/oconnor663/bao"
  url "https://static.crates.io/crates/bao_bin/bao_bin-0.13.1.crate"
  sha256 "77595dfe39d18bf2c3b6a98b0998d5558461653560c209498007f3ca4c392863"
  license any_of: ["Apache-2.0", "CC0-1.0"]
  head "https://github.com/oconnor663/bao.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "369b97e4899c57e85dd468be650613cbc45bc5b4d91144a25a037f331af87b8d"
  end

  depends_on "rust" => :build

  conflicts_with "openbao", because: "both install `bao` binaries"

  deny_network_access!

  def fetch
    cd(build.head? ? "bao_bin" : ".") do
      system "cargo", "generate-lockfile" if build.head?
      system "cargo", "fetch", *std_cargo_fetch_args
    end
  end

  def install
    system "cargo", "install", *std_cargo_args(path: build.head? ? "bao_bin" : ".")
  end

  test do
    test_file = testpath/"test"
    test_file.write "foo"
    output = shell_output("#{bin}/bao hash #{test_file}")
    assert_match "04e0bb39f30b1a3feb89f536c93be15055482df748674b00d26e5a75777702e9", output

    assert_match version.to_s, shell_output("#{bin}/bao --version")
  end
end
