<template>
  <el-form
    ref="formRef"
    :model="formData"
    :rules="rules"
    label-width="80px"
    label-position="right"
    @submit.prevent="handleSubmit"
  >
    <el-form-item label="姓名" prop="name">
      <el-input v-model="formData.name" placeholder="请输入学生姓名" />
    </el-form-item>
    <el-form-item label="学号" prop="studentNumber">
      <el-input v-model="formData.studentNumber" placeholder="请输入学号（可选）" />
    </el-form-item>
    <el-form-item label="性别" prop="gender">
      <el-select v-model="formData.gender" placeholder="请选择性别" clearable>
        <el-option label="男" value="男" />
        <el-option label="女" value="女" />
      </el-select>
    </el-form-item>
    <el-form-item label="所属小组" prop="groupId">
      <el-select v-model="formData.groupId" placeholder="请选择小组" clearable>
        <el-option
          v-for="group in groups"
          :key="group.id"
          :label="group.name"
          :value="group.id"
        />
      </el-select>
    </el-form-item>
    <el-form-item>
      <el-button type="primary" @click="handleSubmit">确定</el-button>
      <el-button @click="$emit('cancel')">取消</el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { Student, StudentGroup } from '@/types'

const props = defineProps<{
  student?: Partial<Student>
  groups: StudentGroup[]
}>()

const emit = defineEmits<{
  submit: [data: Partial<Student>]
  cancel: []
}>()

const formRef = ref<FormInstance>()

const formData = reactive({
  name: props.student?.name ?? '',
  studentNumber: props.student?.studentNumber ?? '',
  gender: props.student?.gender ?? '',
  groupId: props.student?.groupId ?? '',
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入学生姓名', trigger: 'blur' }],
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate((valid) => {
    if (valid) {
      emit('submit', { ...formData, id: props.student?.id })
    }
  })
}
</script>
