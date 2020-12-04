<a name='assembly'></a>
# TTGT

## Contents

- [CUTensor](#T-TTGT-CuTT-CUTensor 'TTGT.CuTT.CUTensor')
  - [#ctor()](#M-TTGT-CuTT-CUTensor-#ctor 'TTGT.CuTT.CUTensor.#ctor')
  - [Contract\`\`1(α,A,modeA,sizeA,β,B,modeB,sizeB,C,modeC,sizeC,D)](#M-TTGT-CuTT-CUTensor-Contract``1-``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}- 'TTGT.CuTT.CUTensor.Contract``1(``0,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],``0,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0})')
  - [Dispose()](#M-TTGT-CuTT-CUTensor-Dispose 'TTGT.CuTT.CUTensor.Dispose')
  - [Permute\`\`1(modeA,α,op,A,sizeA,B,sizeB,modeB)](#M-TTGT-CuTT-CUTensor-Permute``1-CudaCSharp-Memory-Storage{``0},System-Int64[],``0,CudaCSharp-UnitaryOperation,System-Int32[],CudaCSharp-Memory-Storage{``0},System-Int64[],System-Int32[]- 'TTGT.CuTT.CUTensor.Permute``1(CudaCSharp.Memory.Storage{``0},System.Int64[],``0,CudaCSharp.UnitaryOperation,System.Int32[],CudaCSharp.Memory.Storage{``0},System.Int64[],System.Int32[])')
  - [Reduce\`\`1(reduction,α,opA,A,modeA,sizeA,β,opC,C,modeC,sizeC,D)](#M-TTGT-CuTT-CUTensor-Reduce``1-CudaCSharp-BinaryOperation,``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}- 'TTGT.CuTT.CUTensor.Reduce``1(CudaCSharp.BinaryOperation,``0,CudaCSharp.UnitaryOperation,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],``0,CudaCSharp.UnitaryOperation,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0})')
- [Contraction](#T-TTGT-Contraction 'TTGT.Contraction')
  - [CalculateContractionPlan(sizeA,sizeB,sizeC,modeA,modeB,modeC)](#M-TTGT-Contraction-CalculateContractionPlan-System-Int64[],System-Int64[],System-Int64[],System-Int32[],System-Int32[],System-Int32[]- 'TTGT.Contraction.CalculateContractionPlan(System.Int64[],System.Int64[],System.Int64[],System.Int32[],System.Int32[],System.Int32[])')
  - [IsIdentityPermutation(perm)](#M-TTGT-Contraction-IsIdentityPermutation-System-Collections-Generic-IReadOnlyList{System-Int32}- 'TTGT.Contraction.IsIdentityPermutation(System.Collections.Generic.IReadOnlyList{System.Int32})')
  - [IsPermutation(perm)](#M-TTGT-Contraction-IsPermutation-System-Collections-Generic-IReadOnlyList{System-Int32}- 'TTGT.Contraction.IsPermutation(System.Collections.Generic.IReadOnlyList{System.Int32})')
  - [IsTrivialPermute(perm,size)](#M-TTGT-Contraction-IsTrivialPermute-System-Collections-Generic-IReadOnlyList{System-Int32},System-Int64[]- 'TTGT.Contraction.IsTrivialPermute(System.Collections.Generic.IReadOnlyList{System.Int32},System.Int64[])')
- [ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput')
  - [#ctor(sizeA,sizeB,sizeC,modeA,modeB,modeC)](#M-TTGT-ContractionInput-#ctor-System-Int64[],System-Int64[],System-Int64[],System-Char[],System-Char[],System-Char[]- 'TTGT.ContractionInput.#ctor(System.Int64[],System.Int64[],System.Int64[],System.Char[],System.Char[],System.Char[])')
  - [ContractIndex](#P-TTGT-ContractionInput-ContractIndex 'TTGT.ContractionInput.ContractIndex')
  - [LeftContractIndex](#P-TTGT-ContractionInput-LeftContractIndex 'TTGT.ContractionInput.LeftContractIndex')
  - [LeftFreeIndex](#P-TTGT-ContractionInput-LeftFreeIndex 'TTGT.ContractionInput.LeftFreeIndex')
  - [LeftOutFreeIndex](#P-TTGT-ContractionInput-LeftOutFreeIndex 'TTGT.ContractionInput.LeftOutFreeIndex')
  - [LeftSize](#P-TTGT-ContractionInput-LeftSize 'TTGT.ContractionInput.LeftSize')
  - [OutSize](#P-TTGT-ContractionInput-OutSize 'TTGT.ContractionInput.OutSize')
  - [RightContractIndex](#P-TTGT-ContractionInput-RightContractIndex 'TTGT.ContractionInput.RightContractIndex')
  - [RightFreeIndex](#P-TTGT-ContractionInput-RightFreeIndex 'TTGT.ContractionInput.RightFreeIndex')
  - [RightOutFreeIndex](#P-TTGT-ContractionInput-RightOutFreeIndex 'TTGT.ContractionInput.RightOutFreeIndex')
  - [RightSize](#P-TTGT-ContractionInput-RightSize 'TTGT.ContractionInput.RightSize')
  - [Equals(obj)](#M-TTGT-ContractionInput-Equals-System-Object- 'TTGT.ContractionInput.Equals(System.Object)')
  - [GetHashCode()](#M-TTGT-ContractionInput-GetHashCode 'TTGT.ContractionInput.GetHashCode')
  - [TryGetOutputInfoFor(leftPerm,rightPerm,swap)](#M-TTGT-ContractionInput-TryGetOutputInfoFor-System-Int32[],System-Int32[],System-Boolean- 'TTGT.ContractionInput.TryGetOutputInfoFor(System.Int32[],System.Int32[],System.Boolean)')
  - [op_Equality(left,right)](#M-TTGT-ContractionInput-op_Equality-TTGT-ContractionInput,TTGT-ContractionInput- 'TTGT.ContractionInput.op_Equality(TTGT.ContractionInput,TTGT.ContractionInput)')
  - [op_Inequality(left,right)](#M-TTGT-ContractionInput-op_Inequality-TTGT-ContractionInput,TTGT-ContractionInput- 'TTGT.ContractionInput.op_Inequality(TTGT.ContractionInput,TTGT.ContractionInput)')
- [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan')
  - [#ctor()](#M-TTGT-ContractionPlan-#ctor-System-Int32[],System-ValueTuple{System-Int64,System-Int64},System-Boolean,System-Int32[],System-ValueTuple{System-Int64,System-Int64},System-Boolean,System-Boolean,System-Int64[],System-Int32[]- 'TTGT.ContractionPlan.#ctor(System.Int32[],System.ValueTuple{System.Int64,System.Int64},System.Boolean,System.Int32[],System.ValueTuple{System.Int64,System.Int64},System.Boolean,System.Boolean,System.Int64[],System.Int32[])')
  - [EstimationTime](#P-TTGT-ContractionPlan-EstimationTime 'TTGT.ContractionPlan.EstimationTime')
  - [LeftPermute](#P-TTGT-ContractionPlan-LeftPermute 'TTGT.ContractionPlan.LeftPermute')
  - [LeftReshape](#P-TTGT-ContractionPlan-LeftReshape 'TTGT.ContractionPlan.LeftReshape')
  - [LeftTranspose](#P-TTGT-ContractionPlan-LeftTranspose 'TTGT.ContractionPlan.LeftTranspose')
  - [OutPermute](#P-TTGT-ContractionPlan-OutPermute 'TTGT.ContractionPlan.OutPermute')
  - [OutReshape](#P-TTGT-ContractionPlan-OutReshape 'TTGT.ContractionPlan.OutReshape')
  - [RightPermute](#P-TTGT-ContractionPlan-RightPermute 'TTGT.ContractionPlan.RightPermute')
  - [RightReshape](#P-TTGT-ContractionPlan-RightReshape 'TTGT.ContractionPlan.RightReshape')
  - [RightTranspose](#P-TTGT-ContractionPlan-RightTranspose 'TTGT.ContractionPlan.RightTranspose')
  - [SwapLeftRight](#P-TTGT-ContractionPlan-SwapLeftRight 'TTGT.ContractionPlan.SwapLeftRight')
  - [CompareTo(other)](#M-TTGT-ContractionPlan-CompareTo-TTGT-ContractionPlan- 'TTGT.ContractionPlan.CompareTo(TTGT.ContractionPlan)')
  - [CreateAllowViolation(leftPerm,rightPerm,swap,input)](#M-TTGT-ContractionPlan-CreateAllowViolation-System-Int32[],System-Int32[],System-Boolean,TTGT-ContractionInput@- 'TTGT.ContractionPlan.CreateAllowViolation(System.Int32[],System.Int32[],System.Boolean,TTGT.ContractionInput@)')
  - [Equals(obj)](#M-TTGT-ContractionPlan-Equals-System-Object- 'TTGT.ContractionPlan.Equals(System.Object)')
  - [GetHashCode()](#M-TTGT-ContractionPlan-GetHashCode 'TTGT.ContractionPlan.GetHashCode')
  - [op_Equality(left,right)](#M-TTGT-ContractionPlan-op_Equality-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_Equality(TTGT.ContractionPlan,TTGT.ContractionPlan)')
  - [op_GreaterThan(left,right)](#M-TTGT-ContractionPlan-op_GreaterThan-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_GreaterThan(TTGT.ContractionPlan,TTGT.ContractionPlan)')
  - [op_GreaterThanOrEqual(left,right)](#M-TTGT-ContractionPlan-op_GreaterThanOrEqual-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_GreaterThanOrEqual(TTGT.ContractionPlan,TTGT.ContractionPlan)')
  - [op_Inequality(left,right)](#M-TTGT-ContractionPlan-op_Inequality-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_Inequality(TTGT.ContractionPlan,TTGT.ContractionPlan)')
  - [op_LessThan(left,right)](#M-TTGT-ContractionPlan-op_LessThan-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_LessThan(TTGT.ContractionPlan,TTGT.ContractionPlan)')
  - [op_LessThanOrEqual(left,right)](#M-TTGT-ContractionPlan-op_LessThanOrEqual-TTGT-ContractionPlan,TTGT-ContractionPlan- 'TTGT.ContractionPlan.op_LessThanOrEqual(TTGT.ContractionPlan,TTGT.ContractionPlan)')
- [HPTensor](#T-TTGT-HpTT-HPTensor 'TTGT.HpTT.HPTensor')
  - [#ctor()](#M-TTGT-HpTT-HPTensor-#ctor 'TTGT.HpTT.HPTensor.#ctor')
  - [Contract\`\`1(α,A,modeA,sizeA,β,B,modeB,sizeB,C,modeC,sizeC,D)](#M-TTGT-HpTT-HPTensor-Contract``1-``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}- 'TTGT.HpTT.HPTensor.Contract``1(``0,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],``0,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0})')
  - [Dispose()](#M-TTGT-HpTT-HPTensor-Dispose 'TTGT.HpTT.HPTensor.Dispose')
  - [Permute\`\`1(modeA,α,op,A,sizeA,B,sizeB,modeB)](#M-TTGT-HpTT-HPTensor-Permute``1-CudaCSharp-Memory-Storage{``0},System-Int64[],``0,CudaCSharp-UnitaryOperation,System-Int32[],CudaCSharp-Memory-Storage{``0},System-Int64[],System-Int32[]- 'TTGT.HpTT.HPTensor.Permute``1(CudaCSharp.Memory.Storage{``0},System.Int64[],``0,CudaCSharp.UnitaryOperation,System.Int32[],CudaCSharp.Memory.Storage{``0},System.Int64[],System.Int32[])')
  - [Reduce\`\`1(reduction,α,opA,A,modeA,sizeA,β,opC,C,modeC,sizeC,D)](#M-TTGT-HpTT-HPTensor-Reduce``1-CudaCSharp-BinaryOperation,``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}- 'TTGT.HpTT.HPTensor.Reduce``1(CudaCSharp.BinaryOperation,``0,CudaCSharp.UnitaryOperation,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],``0,CudaCSharp.UnitaryOperation,CudaCSharp.Memory.Storage{``0},System.Int32[],System.Int64[],CudaCSharp.Memory.Storage{``0})')
- [NativeMethods](#T-TTGT-CuTT-NativeMethods 'TTGT.CuTT.NativeMethods')
  - [cuttDestroy(handle)](#M-TTGT-CuTT-NativeMethods-cuttDestroy-System-UInt32- 'TTGT.CuTT.NativeMethods.cuttDestroy(System.UInt32)')
  - [cuttExecute(handle,idata,odata)](#M-TTGT-CuTT-NativeMethods-cuttExecute-System-UInt32,System-IntPtr,System-IntPtr- 'TTGT.CuTT.NativeMethods.cuttExecute(System.UInt32,System.IntPtr,System.IntPtr)')
  - [cuttPlan(handle,rank,size,permutation,sizeofType,stream,estTime)](#M-TTGT-CuTT-NativeMethods-cuttPlan-System-UInt32@,System-Int32,System-Int32[],System-Int32[],System-Int64,System-IntPtr,System-Double@- 'TTGT.CuTT.NativeMethods.cuttPlan(System.UInt32@,System.Int32,System.Int32[],System.Int32[],System.Int64,System.IntPtr,System.Double@)')
- [Optimizer](#T-TTGT-Optimizer-Optimizer 'TTGT.Optimizer.Optimizer')
  - [MAOptimize()](#M-TTGT-Optimizer-Optimizer-MAOptimize-TTGT-ContractionInput@,System-Int32- 'TTGT.Optimizer.Optimizer.MAOptimize(TTGT.ContractionInput@,System.Int32)')
- [TensorTranspose\`1](#T-TTGT-HpTT-NativeMethods-TensorTranspose`1 'TTGT.HpTT.NativeMethods.TensorTranspose`1')

<a name='T-TTGT-CuTT-CUTensor'></a>
## CUTensor `type`

##### Namespace

TTGT.CuTT

##### Summary

The class that inherits [ITensor](#T-CudaCSharp-Tensor-ITensor 'CudaCSharp.Tensor.ITensor') with underlying library "CuTT"

<a name='M-TTGT-CuTT-CUTensor-#ctor'></a>
### #ctor() `constructor`

##### Summary

default constructor

##### Parameters

This constructor has no parameters.

<a name='M-TTGT-CuTT-CUTensor-Contract``1-``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}-'></a>
### Contract\`\`1(α,A,modeA,sizeA,β,B,modeB,sizeB,C,modeC,sizeC,D) `method`

##### Summary

Contract two tensors `A` and `B`: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| α | [\`\`0](#T-``0 '``0') | scalar α |
| A | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor A |
| modeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `A` |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `A` |
| β | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | scalar β |
| B | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | tensor B |
| modeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the mode of `B` |
| sizeB | [\`\`0](#T-``0 '``0') | size/extent of `B` |
| C | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor C |
| modeC | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of tensor `D` and `C` |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `C` |
| D | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | output tensor D |

<a name='M-TTGT-CuTT-CUTensor-Dispose'></a>
### Dispose() `method`

##### Summary

default disposition

##### Parameters

This method has no parameters.

<a name='M-TTGT-CuTT-CUTensor-Permute``1-CudaCSharp-Memory-Storage{``0},System-Int64[],``0,CudaCSharp-UnitaryOperation,System-Int32[],CudaCSharp-Memory-Storage{``0},System-Int64[],System-Int32[]-'></a>
### Permute\`\`1(modeA,α,op,A,sizeA,B,sizeB,modeB) `method`

##### Summary

Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| modeA | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | the mode of `A` |
| α | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the scalar to multiply |
| op | [\`\`0](#T-``0 '``0') | the [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`Ψ` to apply on each element before scaling |
| A | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | the source tensor |
| sizeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | size/extent of `A` |
| B | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | the output tensor |
| sizeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `B` |
| modeB | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `B` |

<a name='M-TTGT-CuTT-CUTensor-Reduce``1-CudaCSharp-BinaryOperation,``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}-'></a>
### Reduce\`\`1(reduction,α,opA,A,modeA,sizeA,β,opC,C,modeC,sizeC,D) `method`

##### Summary

Partial reduction of tensor `A`: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The missing indices of `modeA` from `modeC` will be aggregated according to `reduction`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| reduction | [CudaCSharp.BinaryOperation](#T-CudaCSharp-BinaryOperation 'CudaCSharp.BinaryOperation') | the reduce [BinaryOperation](#T-CudaCSharp-BinaryOperation 'CudaCSharp.BinaryOperation')`Φ` |
| α | [\`\`0](#T-``0 '``0') | scalar α |
| opA | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`ΨA` |
| A | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor A |
| modeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `A` |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `A` |
| β | [\`\`0](#T-``0 '``0') | scalar β, default 0 |
| opC | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`ΨC` |
| C | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor C |
| modeC | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of tensor `D` and `C` |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `C` |
| D | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | output tensor D |

##### Remarks

currently, this method is implemented via GEMV which only support `reduction` == [Add](#F-CudaCSharp-BinaryOperation-Add 'CudaCSharp.BinaryOperation.Add'), and may cause some performance loss

<a name='T-TTGT-Contraction'></a>
## Contraction `type`

##### Namespace

TTGT

##### Summary

The contraction related static methods and struct

<a name='M-TTGT-Contraction-CalculateContractionPlan-System-Int64[],System-Int64[],System-Int64[],System-Int32[],System-Int32[],System-Int32[]-'></a>
### CalculateContractionPlan(sizeA,sizeB,sizeC,modeA,modeB,modeC) `method`

##### Summary

Calculate the TTGT (Tensor transpose-Tensor transpose-GEMM-Tensor transpose) contraction plan of `C += A * B`

##### Returns

the plan as [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan')

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the size/extent of `A` |
| sizeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the size/extent of `B` |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the size/extent of `C` |
| modeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `A` |
| modeB | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `B` |
| modeC | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `C` |

<a name='M-TTGT-Contraction-IsIdentityPermutation-System-Collections-Generic-IReadOnlyList{System-Int32}-'></a>
### IsIdentityPermutation(perm) `method`

##### Summary

Check if `perm` is an identity permutation or not.

##### Returns

`perm` is an identity permutation or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| perm | [System.Collections.Generic.IReadOnlyList{System.Int32}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IReadOnlyList 'System.Collections.Generic.IReadOnlyList{System.Int32}') | the permutation to check |

<a name='M-TTGT-Contraction-IsPermutation-System-Collections-Generic-IReadOnlyList{System-Int32}-'></a>
### IsPermutation(perm) `method`

##### Summary

Check if `perm` is a permutation or not.

##### Returns

`perm` is a permutation or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| perm | [System.Collections.Generic.IReadOnlyList{System.Int32}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IReadOnlyList 'System.Collections.Generic.IReadOnlyList{System.Int32}') | the permutation to check |

<a name='M-TTGT-Contraction-IsTrivialPermute-System-Collections-Generic-IReadOnlyList{System-Int32},System-Int64[]-'></a>
### IsTrivialPermute(perm,size) `method`

##### Summary

Check if `perm` is a trivial (can be achieved by matrix transposition) permutation. If it is, the corresponding matrix shape will be returned. Otherwise, null will be returned.

##### Returns

Null if `perm` is not trivial or the corresponding matrix shape.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| perm | [System.Collections.Generic.IReadOnlyList{System.Int32}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IReadOnlyList 'System.Collections.Generic.IReadOnlyList{System.Int32}') | the input permutation as an integer array, e.g. {2,0,1} |
| size | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the size/extent of the target tensor, can be null or empty if you don't want the shape |

##### Remarks

This is equivalent to find whether `perm` is a cycle composed of identity permutation

<a name='T-TTGT-ContractionInput'></a>
## ContractionInput `type`

##### Namespace

TTGT

##### Summary

The contraction input struct

<a name='M-TTGT-ContractionInput-#ctor-System-Int64[],System-Int64[],System-Int64[],System-Char[],System-Char[],System-Char[]-'></a>
### #ctor(sizeA,sizeB,sizeC,modeA,modeB,modeC) `constructor`

##### Summary

Construct from size and mode (of [Char](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Char 'System.Char') array)

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | left tensor's size/extent |
| sizeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | right tensor's size/extent |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | output tensor's size/extent |
| modeA | [System.Char[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Char[] 'System.Char[]') | left tensor's mode |
| modeB | [System.Char[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Char[] 'System.Char[]') | right tensor's mode |
| modeC | [System.Char[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Char[] 'System.Char[]') | output tensor's mode |

<a name='P-TTGT-ContractionInput-ContractIndex'></a>
### ContractIndex `property`

##### Summary

The contract indices

<a name='P-TTGT-ContractionInput-LeftContractIndex'></a>
### LeftContractIndex `property`

##### Summary

The contract indices of left tensor

<a name='P-TTGT-ContractionInput-LeftFreeIndex'></a>
### LeftFreeIndex `property`

##### Summary

The left tensor's free index

<a name='P-TTGT-ContractionInput-LeftOutFreeIndex'></a>
### LeftOutFreeIndex `property`

##### Summary

The left tensor's free index and corresponding output tensor's (index in left tensor, index in output tensor)

<a name='P-TTGT-ContractionInput-LeftSize'></a>
### LeftSize `property`

##### Summary

The left tensor's size / extent

<a name='P-TTGT-ContractionInput-OutSize'></a>
### OutSize `property`

##### Summary

The output tensor's size / extent

<a name='P-TTGT-ContractionInput-RightContractIndex'></a>
### RightContractIndex `property`

##### Summary

The contract indices of right tensor

<a name='P-TTGT-ContractionInput-RightFreeIndex'></a>
### RightFreeIndex `property`

##### Summary

The right tensor's free index

<a name='P-TTGT-ContractionInput-RightOutFreeIndex'></a>
### RightOutFreeIndex `property`

##### Summary

The right tensor's free index and corresponding output tensor's (index in right tensor, index in output tensor)

<a name='P-TTGT-ContractionInput-RightSize'></a>
### RightSize `property`

##### Summary

The right tensor's size / extent

<a name='M-TTGT-ContractionInput-Equals-System-Object-'></a>
### Equals(obj) `method`

##### Summary

Indicates whether this [ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') and a specified object are equal.

##### Returns

true if obj and this instance are the same type and represent the same value

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| obj | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') | The object to compare with the current instance. |

<a name='M-TTGT-ContractionInput-GetHashCode'></a>
### GetHashCode() `method`

##### Summary

Returns the hash code for this [ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput').

##### Returns

A [Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') that is the hash code for this [ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput').

##### Parameters

This method has no parameters.

<a name='M-TTGT-ContractionInput-TryGetOutputInfoFor-System-Int32[],System-Int32[],System-Boolean-'></a>
### TryGetOutputInfoFor(leftPerm,rightPerm,swap) `method`

##### Summary

Try to get the direct output tensor's (the tensor after matrix multiplication) size and permutation needed to convert to the desired output tensor

##### Returns

the direct output's size and permutation needed to convert to the desired output tensor, or null if the input is invalid

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| leftPerm | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the left tensor's permutation |
| rightPerm | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the right tensor's permutation |
| swap | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | swap left and right tensor or not |

<a name='M-TTGT-ContractionInput-op_Equality-TTGT-ContractionInput,TTGT-ContractionInput-'></a>
### op_Equality(left,right) `method`

##### Summary

Equality operator

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') |  |
| right | [TTGT.ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') |  |

<a name='M-TTGT-ContractionInput-op_Inequality-TTGT-ContractionInput,TTGT-ContractionInput-'></a>
### op_Inequality(left,right) `method`

##### Summary

Not-equality operator

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') |  |
| right | [TTGT.ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') |  |

<a name='T-TTGT-ContractionPlan'></a>
## ContractionPlan `type`

##### Namespace

TTGT

##### Summary

The contraction plan struct

<a name='M-TTGT-ContractionPlan-#ctor-System-Int32[],System-ValueTuple{System-Int64,System-Int64},System-Boolean,System-Int32[],System-ValueTuple{System-Int64,System-Int64},System-Boolean,System-Boolean,System-Int64[],System-Int32[]-'></a>
### #ctor() `constructor`

##### Summary

Direct constructor

##### Parameters

This constructor has no parameters.

##### Remarks

since sizes/extents of left and right tensors are not indicated, the [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') constructed may still not be a valid one

<a name='P-TTGT-ContractionPlan-EstimationTime'></a>
### EstimationTime `property`

##### Summary

The estimated execution time of this plan, in seconds, can only be used for comparison

<a name='P-TTGT-ContractionPlan-LeftPermute'></a>
### LeftPermute `property`

##### Summary

Procedure 1, permute left tensor

<a name='P-TTGT-ContractionPlan-LeftReshape'></a>
### LeftReshape `property`

##### Summary

Procedure 2, reshape left tensor to matrix

<a name='P-TTGT-ContractionPlan-LeftTranspose'></a>
### LeftTranspose `property`

##### Summary

Procedure 3, GEMM of left (and right) matrix, do transpose or not

<a name='P-TTGT-ContractionPlan-OutPermute'></a>
### OutPermute `property`

##### Summary

Procedure 5, permute output tensor

<a name='P-TTGT-ContractionPlan-OutReshape'></a>
### OutReshape `property`

##### Summary

Procedure 4, reshape output matrix to tensor

<a name='P-TTGT-ContractionPlan-RightPermute'></a>
### RightPermute `property`

##### Summary

Procedure 1, permute right tensor

<a name='P-TTGT-ContractionPlan-RightReshape'></a>
### RightReshape `property`

##### Summary

Procedure 2, reshape right tensor to matrix

<a name='P-TTGT-ContractionPlan-RightTranspose'></a>
### RightTranspose `property`

##### Summary

Procedure 3, GEMM of right (and left) matrix, do transpose or not

<a name='P-TTGT-ContractionPlan-SwapLeftRight'></a>
### SwapLeftRight `property`

##### Summary

Procedure 3, GEMM of right (and left) matrix, swap left and right or not

<a name='M-TTGT-ContractionPlan-CompareTo-TTGT-ContractionPlan-'></a>
### CompareTo(other) `method`

##### Summary

Compare the estimated execution time of this plan to the `other` plan

##### Returns

0 if `this == `; above zero if the estimation time cost of `this > `; below zero otherwise

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| other | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') | the other plan |

<a name='M-TTGT-ContractionPlan-CreateAllowViolation-System-Int32[],System-Int32[],System-Boolean,TTGT-ContractionInput@-'></a>
### CreateAllowViolation(leftPerm,rightPerm,swap,input) `method`

##### Summary

Create a [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') allowing violation of constraints using the minimal number of parameters with respect to the input arguments `input`

##### Returns

a [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') if the input is valid; or a [Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') indicates how much it violates the constraints

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| leftPerm | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the permutation of left tensor |
| rightPerm | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the permutation of right tensor |
| swap | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | swap the tensors or not |
| input | [TTGT.ContractionInput@](#T-TTGT-ContractionInput@ 'TTGT.ContractionInput@') | the constant input arguments in [ContractionInput](#T-TTGT-ContractionInput 'TTGT.ContractionInput') |

<a name='M-TTGT-ContractionPlan-Equals-System-Object-'></a>
### Equals(obj) `method`

##### Summary

Indicates whether this [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') and a specified object are equal.

##### Returns

true if obj and this instance are the same type and represent the same value

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| obj | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') | The object to compare with the current instance. |

<a name='M-TTGT-ContractionPlan-GetHashCode'></a>
### GetHashCode() `method`

##### Summary

Returns the hash code for this [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan').

##### Returns

A [Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') that is the hash code for this [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan').

##### Parameters

This method has no parameters.

<a name='M-TTGT-ContractionPlan-op_Equality-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_Equality(left,right) `method`

##### Summary

Equality operator

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='M-TTGT-ContractionPlan-op_GreaterThan-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_GreaterThan(left,right) `method`

##### Summary

Larger operator

##### Returns

`left` has larger [EstimationTime](#P-TTGT-ContractionPlan-EstimationTime 'TTGT.ContractionPlan.EstimationTime') than `right` or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='M-TTGT-ContractionPlan-op_GreaterThanOrEqual-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_GreaterThanOrEqual(left,right) `method`

##### Summary

Larger or equal operator

##### Returns

`left` has larger or the same [EstimationTime](#P-TTGT-ContractionPlan-EstimationTime 'TTGT.ContractionPlan.EstimationTime') than `right` or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='M-TTGT-ContractionPlan-op_Inequality-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_Inequality(left,right) `method`

##### Summary

Not-equality operator

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='M-TTGT-ContractionPlan-op_LessThan-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_LessThan(left,right) `method`

##### Summary

Smaller operator

##### Returns

`left` has smaller [EstimationTime](#P-TTGT-ContractionPlan-EstimationTime 'TTGT.ContractionPlan.EstimationTime') than `right` or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='M-TTGT-ContractionPlan-op_LessThanOrEqual-TTGT-ContractionPlan,TTGT-ContractionPlan-'></a>
### op_LessThanOrEqual(left,right) `method`

##### Summary

Smaller or equal operator

##### Returns

`left` has smaller or the same [EstimationTime](#P-TTGT-ContractionPlan-EstimationTime 'TTGT.ContractionPlan.EstimationTime') than `right` or not

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| left | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |
| right | [TTGT.ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan') |  |

<a name='T-TTGT-HpTT-HPTensor'></a>
## HPTensor `type`

##### Namespace

TTGT.HpTT

##### Summary

The class that inherits [ITensor](#T-CudaCSharp-Tensor-ITensor 'CudaCSharp.Tensor.ITensor') with underlying library "HpTT"

<a name='M-TTGT-HpTT-HPTensor-#ctor'></a>
### #ctor() `constructor`

##### Summary

default constructor

##### Parameters

This constructor has no parameters.

<a name='M-TTGT-HpTT-HPTensor-Contract``1-``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}-'></a>
### Contract\`\`1(α,A,modeA,sizeA,β,B,modeB,sizeB,C,modeC,sizeC,D) `method`

##### Summary

Contract two tensors `A` and `B`: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| α | [\`\`0](#T-``0 '``0') | scalar α |
| A | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor A |
| modeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `A` |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `A` |
| β | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | scalar β |
| B | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | tensor B |
| modeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the mode of `B` |
| sizeB | [\`\`0](#T-``0 '``0') | size/extent of `B` |
| C | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor C |
| modeC | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of tensor `D` and `C` |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `C` |
| D | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | output tensor D |

<a name='M-TTGT-HpTT-HPTensor-Dispose'></a>
### Dispose() `method`

##### Summary

default disposition

##### Parameters

This method has no parameters.

<a name='M-TTGT-HpTT-HPTensor-Permute``1-CudaCSharp-Memory-Storage{``0},System-Int64[],``0,CudaCSharp-UnitaryOperation,System-Int32[],CudaCSharp-Memory-Storage{``0},System-Int64[],System-Int32[]-'></a>
### Permute\`\`1(modeA,α,op,A,sizeA,B,sizeB,modeB) `method`

##### Summary

Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| modeA | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | the mode of `A` |
| α | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | the scalar to multiply |
| op | [\`\`0](#T-``0 '``0') | the [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`Ψ` to apply on each element before scaling |
| A | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | the source tensor |
| sizeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | size/extent of `A` |
| B | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | the output tensor |
| sizeB | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `B` |
| modeB | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `B` |

<a name='M-TTGT-HpTT-HPTensor-Reduce``1-CudaCSharp-BinaryOperation,``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],``0,CudaCSharp-UnitaryOperation,CudaCSharp-Memory-Storage{``0},System-Int32[],System-Int64[],CudaCSharp-Memory-Storage{``0}-'></a>
### Reduce\`\`1(reduction,α,opA,A,modeA,sizeA,β,opC,C,modeC,sizeC,D) `method`

##### Summary

Partial reduction of tensor `A`: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The missing indices of `modeA` from `modeC` will be aggregated according to `reduction`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| reduction | [CudaCSharp.BinaryOperation](#T-CudaCSharp-BinaryOperation 'CudaCSharp.BinaryOperation') | the reduce [BinaryOperation](#T-CudaCSharp-BinaryOperation 'CudaCSharp.BinaryOperation')`Φ` |
| α | [\`\`0](#T-``0 '``0') | scalar α |
| opA | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`ΨA` |
| A | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor A |
| modeA | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of `A` |
| sizeA | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `A` |
| β | [\`\`0](#T-``0 '``0') | scalar β, default 0 |
| opC | [CudaCSharp.UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation') | [UnitaryOperation](#T-CudaCSharp-UnitaryOperation 'CudaCSharp.UnitaryOperation')`ΨC` |
| C | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | tensor C |
| modeC | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | the mode of tensor `D` and `C` |
| sizeC | [System.Int64[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64[] 'System.Int64[]') | size/extent of `C` |
| D | [CudaCSharp.Memory.Storage{\`\`0}](#T-CudaCSharp-Memory-Storage{``0} 'CudaCSharp.Memory.Storage{``0}') | output tensor D |

##### Remarks

currently, this method is implemented via GEMV which only support `reduction` == [Add](#F-CudaCSharp-BinaryOperation-Add 'CudaCSharp.BinaryOperation.Add'), and may cause some performance loss

<a name='T-TTGT-CuTT-NativeMethods'></a>
## NativeMethods `type`

##### Namespace

TTGT.CuTT

<a name='M-TTGT-CuTT-NativeMethods-cuttDestroy-System-UInt32-'></a>
### cuttDestroy(handle) `method`

##### Summary

Destroy the plan

##### Returns

Success/unsuccessful code [CuTTResult](#T-TTGT-CuTT-CuTTResult 'TTGT.CuTT.CuTTResult')

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| handle | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Handle to the cuTT plan |

<a name='M-TTGT-CuTT-NativeMethods-cuttExecute-System-UInt32,System-IntPtr,System-IntPtr-'></a>
### cuttExecute(handle,idata,odata) `method`

##### Summary

Execute plan out-of-place

##### Returns

Success/unsuccessful code [CuTTResult](#T-TTGT-CuTT-CuTTResult 'TTGT.CuTT.CuTTResult')

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| handle | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Handle to cuTT plan |
| idata | [System.IntPtr](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.IntPtr 'System.IntPtr') | Input data |
| odata | [System.IntPtr](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.IntPtr 'System.IntPtr') | Output data |

<a name='M-TTGT-CuTT-NativeMethods-cuttPlan-System-UInt32@,System-Int32,System-Int32[],System-Int32[],System-Int64,System-IntPtr,System-Double@-'></a>
### cuttPlan(handle,rank,size,permutation,sizeofType,stream,estTime) `method`

##### Summary

Create the permutation plan using heuristic method

##### Returns

Success/unsuccessful code [CuTTResult](#T-TTGT-CuTT-CuTTResult 'TTGT.CuTT.CuTTResult')

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| handle | [System.UInt32@](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32@ 'System.UInt32@') | Returned handle to cuTT plan |
| rank | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Rank of the tensor |
| size | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | Dimensions / size of the tensor |
| permutation | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | Transpose permutation, e.g. {0,3,1,2} |
| sizeofType | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Size of the elements of the tensor in bytes (must 4 or 8) |
| stream | [System.IntPtr](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.IntPtr 'System.IntPtr') | CUDA stream (0 if no stream is used) |
| estTime | [System.Double@](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double@ 'System.Double@') | returned estimation execution time |

<a name='T-TTGT-Optimizer-Optimizer'></a>
## Optimizer `type`

##### Namespace

TTGT.Optimizer

##### Summary

The optimizer for [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan')

<a name='M-TTGT-Optimizer-Optimizer-MAOptimize-TTGT-ContractionInput@,System-Int32-'></a>
### MAOptimize() `method`

##### Summary

The memetic algorithm (MA) optimizer for [ContractionPlan](#T-TTGT-ContractionPlan 'TTGT.ContractionPlan')

##### Parameters

This method has no parameters.

##### Remarks

In computer science and operations research, a memetic algorithm (MA) is an extension of the traditional genetic algorithm. It uses a local search technique to reduce the likelihood of the premature convergence.

<a name='T-TTGT-HpTT-NativeMethods-TensorTranspose`1'></a>
## TensorTranspose\`1 `type`

##### Namespace

TTGT.HpTT.NativeMethods

##### Summary

Computes the out-of-place tensor transposition of A into B.
A tensor transposition plan is a data structure that encodes the execution of the tensor transposition.
HPTT supports tensor transpositions of the form:
$B_{\pi(i_0, i_1,...)} = \alpha * A_ { i_0,i_1,...} + \beta * B_ {\pi(i_0,i_1,...)}.$

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| perm | [T:TTGT.HpTT.NativeMethods.TensorTranspose\`1](#T-T-TTGT-HpTT-NativeMethods-TensorTranspose`1 'T:TTGT.HpTT.NativeMethods.TensorTranspose`1') | permutation of size `dim` representing the permutation of the indices. For instance, perm[] = { 1, 0, 2 } denotes the following transposition: $B_{i1,i0,i2} := A_ { i0,i1,i2 }$. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | the data type |
