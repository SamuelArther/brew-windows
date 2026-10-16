class Llmfit < Formula
  desc "Find what models run on your hardware"
  homepage "https://github.com/AlexsJones/llmfit"
  url "https://github.com/AlexsJones/llmfit/releases/download/v1.1.17/llmfit-v1.1.17-x86_64-pc-windows-msvc.zip"
  sha256 "661b6ea57d835f681fbabcf3366649ff681d0081267fd3a05db254e67af6073f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://static.crates.io/crates/llmfit/llmfit-1.1.17.crate"
  end

  def install
    bin.install "llmfit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/llmfit --version")
    assert_match(/Found \d+ model\(s\)/i, shell_output("#{bin}/llmfit search llama"))
  end
end
