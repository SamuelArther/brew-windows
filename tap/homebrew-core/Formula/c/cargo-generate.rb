class CargoGenerate < Formula
  desc "Use pre-existing git repositories as templates"
  homepage "https://github.com/cargo-generate/cargo-generate"
  url "https://github.com/cargo-generate/cargo-generate/archive/refs/tags/v0.25.1.tar.gz"
  sha256 "a36b52c52a8a1c62f8f53a7e9d6d88ea84dd917dc8b2e6523a423b1fc32d5c53"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/cargo-generate/cargo-generate.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7513e466aa370963183666edf0c15df3915380f7faf492181418dc3896f6030b"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match "No favorites defined", shell_output("#{bin}/cargo-generate gen --list-favorites")

    system bin/"cargo-generate", "gen", "--git", "https://github.com/ashleygwilliams/wasm-pack-template",
                                 "--name", "brewtest"
    assert_path_exists testpath/"brewtest"
    assert_match "brewtest", (testpath/"brewtest/Cargo.toml").read
  end
end
