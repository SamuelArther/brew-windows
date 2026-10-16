class Solidity < Formula
  desc "Contract-oriented programming language"
  homepage "https://soliditylang.org"
  url "https://github.com/argotorg/solidity/releases/download/v0.8.37/solc-windows.exe"
  sha256 "967743b2d12155f59a0ddf9410afe25c7deaa73f4fe3d33c4dc0739f627d9ec9"
  license all_of: ["GPL-3.0-or-later", "MIT", "BSD-3-Clause", "Apache-2.0", "CC0-1.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/argotorg/solidity/releases/download/v0.8.37/solidity_0.8.37.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "solc-windows.exe" => "solc.exe"
  end

  test do
    (testpath/"hello.sol").write <<~SOLIDITY
      // SPDX-License-Identifier: GPL-3.0
      pragma solidity ^0.8.0;
      contract HelloWorld {
        function helloWorld() external pure returns (string memory) {
          return "Hello, World!";
        }
      }
    SOLIDITY

    output = shell_output("#{bin}/solc --bin hello.sol")
    assert_match "hello.sol:HelloWorld", output
    assert_match "Binary:", output
  end
end
