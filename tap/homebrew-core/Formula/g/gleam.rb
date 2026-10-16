class Gleam < Formula
  desc "Statically typed language for the Erlang VM"
  homepage "https://gleam.run"
  url "https://github.com/gleam-lang/gleam/releases/download/v1.19.2/gleam-v1.19.2-x86_64-pc-windows-msvc.zip"
  sha256 "21744a7fb040f64142c1b45b7a0149bf2c46b4aa46b435abd35848f9821ce729"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gleam-lang/gleam/archive/refs/tags/v1.19.2.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "gleam.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gleam --version 2>&1")
  end
end
