class Truffle < Formula
  desc "Development environment, testing framework and asset pipeline for Ethereum"
  homepage "https://trufflesuite.com"
  url "https://registry.npmjs.org/truffle/-/truffle-5.11.5.tgz"
  sha256 "bbc24698fc9964cd80acc8952500f708ef18984096eba9e75f40db3486392347"
  license "MIT"

  # https://consensys.io/blog/consensys-announces-the-sunset-of-truffle-and-ganache-and-new-hardhat
  deprecate! date: "2025-11-22", because: :unsupported
  disable! date: "2026-09-16", because: :unsupported

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]

    # Remove incompatible pre-built binaries
    truffle_dir = libexec/"lib/node_modules/truffle"
    os = OS.kernel_name.downcase
    arch = Hardware::CPU.intel? ? "x64" : Hardware::CPU.arch.to_s
    platforms = ["#{os}-#{arch}"]
    platforms << "#{os}-x64+arm64" if OS.mac?
    %w[
      **/node_modules/*
      node_modules/ganache/node_modules/@trufflesuite/bigint-buffer
    ].each do |pattern|
      truffle_dir.glob("#{pattern}/prebuilds/*").each do |dir|
        dir.glob("*.musl.node").map(&:unlink)
        rm_r(dir) if platforms.exclude?(dir.basename.to_s)
      end
    end
    pattern = "node_modules/ganache/node_modules/@trufflesuite/uws-js-unofficial/{binaries,dist}/*.node"
    truffle_dir.glob(pattern) do |f|
      rm(f) unless f.basename.to_s.match?("_#{os}_#{arch}_")
    end

    # Remove incompatible pre-built binaries that have arbitrary names
    truffle_dir.glob("node_modules/ganache/dist/node{/,/F/}*.node").each do |f|
      next unless f.dylib?
      next if f.arch == Hardware::CPU.arch
      next if OS.mac? && f.archs.include?(Hardware::CPU.arch)

      f.unlink
    end

    # Replace universal binaries with their native slices
    deuniversalize_machos
  end

  test do
    system bin/"truffle", "init"
    system bin/"truffle", "compile"
    system bin/"truffle", "test"
  end
end
