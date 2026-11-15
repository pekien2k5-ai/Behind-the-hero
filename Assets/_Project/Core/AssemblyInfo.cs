using System.Runtime.CompilerServices;

// Lớp Core chạy được ngoài Unity (Tools/CoreTests) và trong Unity Test Runner.
// Test assembly cần chạm vào setter nội bộ để dựng state thay cho action thật.
[assembly: InternalsVisibleTo("BTH.Core.Tests")]
