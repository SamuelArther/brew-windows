class Sentencepiece < Formula
  desc "Unsupervised text tokenizer and detokenizer"
  homepage "https://github.com/google/sentencepiece"
  url "https://github.com/google/sentencepiece/releases/download/v0.2.2/sentencepiece-0.2.2-Windows-AMD64.7z"
  sha256 "231940ac1a414babfc3c7ea40a864c272a41f79d639b1109bdc58417fc9bd719"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/sentencepiece/archive/refs/tags/v0.2.2.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/spm_decode.exe", "spm_decode"
    windows_exe "libexec/bin/spm_encode.exe", "spm_encode"
    windows_exe "libexec/bin/spm_export_vocab.exe", "spm_export_vocab"
    windows_exe "libexec/bin/spm_normalize.exe", "spm_normalize"
    windows_exe "libexec/bin/spm_train.exe", "spm_train"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spm_decode --version 2>&1")
  end
end
