class ElmFormat < Formula
  desc "Elm source code formatter, inspired by gofmt"
  homepage "https://github.com/avh4/elm-format"
  url "https://github.com/avh4/elm-format/releases/download/0.8.8/elm-format-0.8.8-win-x64.zip"
  sha256 "4a2d0eb0529944fbfb7974943df5a03881ec4b67501a631e1272aa9fd746bc82"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/avh4/elm-format.git"
  end

  def install
    bin.install "elm-format.exe"
  end

  test do
    src_path = testpath/"Hello.elm"
    src_path.write <<~ELM
      import Html exposing (text)
      main = text "Hello, world!"
    ELM

    system bin/"elm-format", "--elm-version=0.18", testpath/"Hello.elm", "--yes"
    system bin/"elm-format", "--elm-version=0.19", testpath/"Hello.elm", "--yes"

    assert_match version.to_s, shell_output("#{bin}/elm-format --help")
  end
end
