class CargoFuzz < Formula
  desc "Command-line helpers for fuzzing"
  homepage "https://rust-fuzz.github.io/book/cargo-fuzz.html"
  url "https://github.com/rust-fuzz/cargo-fuzz/releases/download/0.13.2/cargo-fuzz-0.13.2-x86_64-pc-windows-msvc.zip"
  sha256 "c4282b9f938368f711c0d959e7fcf1cd52fc40e233425c334f16012f5a7457a8"
  license all_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rust-fuzz/cargo-fuzz/archive/refs/tags/0.13.2.tar.gz"
    strategy :github_releases
  end

  def install
    bin.install "cargo-fuzz.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cargo-fuzz --version 2>&1")
  end
end
